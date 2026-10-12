using Construction.Application.Common.Exceptions;
using Construction.Application.Features.BookImport;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.LockLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Importing a month of the old spreadsheet. Each test uses a year of its own, and names that no other test
/// uses, so people and clients of other tests cannot be matched by accident.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BookImportTests : IntegrationTestBase
{
    public BookImportTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static int nextYear = 2060;

    private static int FreshYear() => Interlocked.Increment(ref nextYear);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<T> AsSuperAdminAsync<T>(Func<TestScope, Task<T>> action)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

            return await action(scope);
        });
    }

    private sealed record Seeded(string Client, Guid ProjectId, string First, string Last, Guid EmployeeId);

    private Task<Seeded> SeedAsync() =>
        InScope(async scope =>
        {
            var tag = Unique();
            var project = await TestData.SeedProjectAsync(scope, name: $"Klijent {tag}");
            var employee = await TestData.SeedEmployeeAsync(scope);

            employee.FirstName = $"Ime{tag}";
            employee.LastName = $"Prezime{tag}";
            await scope.Db.SaveChangesAsync();

            return new Seeded(project.Name, project.Id, employee.FirstName, employee.LastName, employee.Id);
        });

    private static BookImportRowInput Row(
        string section, string label, string[] weeks, string? workerRate = null, string? clientRate = "33",
        string? advance = null) =>
        new()
        {
            Section = section,
            Label = label,
            Weeks = weeks,
            WorkerRate = workerRate,
            ClientRate = clientRate,
            Advance = advance,
        };

    private static BookImportInput Input(int year, params BookImportRowInput[] rows) =>
        new() { FileName = "oktobar.xlsx", Year = year, Month = 10, Rows = rows };

    [Fact]
    public async Task The_preview_matches_people_and_clients_and_reports_each_row()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var plan = await AsSuperAdminAsync(scope => scope.Send(new PreviewBookImportCommand(Input(
            year,
            Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40", "40", "40", "40"], "20"),
            Row(seeded.Client, "Nepoznat Radnik", ["10"], "15"),
            Row("Nepoznat klijent " + Unique(), $"{seeded.Last} {seeded.First}", ["8"], "20")))));

        Assert.Equal(3, plan.Rows.Count);

        var matched = plan.Rows[0];
        Assert.Equal(seeded.EmployeeId, matched.EmployeeId);
        Assert.Equal(seeded.ProjectId, matched.ProjectId);
        Assert.Equal("Ok", matched.Status);
        Assert.Equal(160m, matched.Hours);

        var unknownPerson = plan.Rows[1];
        Assert.Null(unknownPerson.EmployeeId);
        Assert.Equal("Warning", unknownPerson.Status);

        // Last name first still finds the person; the client was not found, which is a warning.
        var unknownClient = plan.Rows[2];
        Assert.Equal(seeded.EmployeeId, unknownClient.EmployeeId);
        Assert.Null(unknownClient.ProjectId);
        Assert.Equal("Warning", unknownClient.Status);
        Assert.Single(plan.UnmatchedSections);

        Assert.Equal(0, plan.ErrorCount);
        Assert.Equal(178m, plan.SheetHours);
        Assert.Equal(178m * 33m, plan.SheetBilling);
    }

    [Fact]
    public async Task A_row_that_is_not_a_number_or_repeats_a_person_is_an_error_and_is_left_out()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var result = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(Input(
            year,
            Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40", "oko"], "20"),
            Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40"], "20"),
            Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40"], "20")))));

        // The first has an unreadable number, the third repeats the person of the second.
        Assert.Equal("Error", result.Plan.Rows[0].Status);
        Assert.Equal("Ok", result.Plan.Rows[1].Status);
        Assert.Equal("Error", result.Plan.Rows[2].Status);
        Assert.Equal(1, result.Imported);
        Assert.Equal(2, result.Skipped);
    }

    [Fact]
    public async Task An_import_makes_the_settlement_and_the_totals_agree_with_the_sheet()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var result = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(Input(
            year,
            Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40", "40", "40", "40"], "20", "33", "500")))));

        Assert.True(result.LedgerCreated);
        Assert.Equal(1, result.Imported);
        Assert.Equal(160m, result.LedgerHours);
        Assert.Equal(0m, result.HoursDifference);
        Assert.Equal(5280m, result.LedgerBilling);
        Assert.Equal(0m, result.BillingDifference);

        var stored = await InScope(async scope =>
        {
            var sections = await scope.Db.LedgerSections
                .Include(s => s.Rows).ThenInclude(r => r.Cells)
                .Where(s => s.LedgerId == result.LedgerId)
                .ToListAsync();

            return sections;
        });

        var section = Assert.Single(stored);
        Assert.Equal(seeded.ProjectId, section.ProjectId);

        var row = Assert.Single(section.Rows);
        Assert.Equal(seeded.EmployeeId, row.EmployeeId);
    }

    [Fact]
    public async Task Importing_the_same_file_again_into_the_empty_cells_does_not_duplicate_anything()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var input = Input(
            year,
            Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40", "40"], "20"));

        var first = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(input)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(input))));

        var second = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(
            new BookImportInput
            {
                FileName = input.FileName, Year = input.Year, Month = input.Month,
                Rows = input.Rows, FillEmptyOnly = true,
            })));

        Assert.False(second.LedgerCreated);
        Assert.Equal(first.LedgerId, second.LedgerId);
        Assert.Equal(80m, second.LedgerHours);

        var rows = await InScope(scope => scope.Db.LedgerRows
            .CountAsync(r => r.Section.LedgerId == first.LedgerId));

        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task Filling_empty_cells_leaves_what_was_already_typed()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var created = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(Input(
            year, Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40"], "20")))));

        // Somebody corrects the hours by hand, then the file is brought in again with the old figure.
        await AsSuperAdminAsync(async scope =>
        {
            var cell = await scope.Db.LedgerCells
                .Where(c => c.Row.Section.LedgerId == created.LedgerId && c.Column.SystemKey == "week1")
                .SingleAsync();

            await scope.Send(new SetLedgerCellCommand { RowId = cell.RowId, ColumnId = cell.ColumnId, Value = "52" });

            return 0;
        });

        var again = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(new BookImportInput
        {
            FileName = "oktobar.xlsx", Year = year, Month = 10, FillEmptyOnly = true,
            Rows = [Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40", "10"], "20")],
        })));

        // Week 1 keeps the corrected 52; the empty week 2 is filled with 10.
        Assert.Equal(62m, again.LedgerHours);
    }

    [Fact]
    public async Task An_import_that_made_the_month_can_be_taken_back_until_somebody_changes_it()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var result = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(Input(
            year, Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40"], "20")))));

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new UndoBookImportCommand(result.BatchId));
            return 0;
        });

        var remains = await InScope(scope => scope.Db.Ledgers.AnyAsync(l => l.Id == result.LedgerId));
        Assert.False(remains);

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new UndoBookImportCommand(result.BatchId));
            return 0;
        }));
    }

    [Fact]
    public async Task A_month_changed_by_hand_or_closed_cannot_be_taken_back()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var result = await AsSuperAdminAsync(scope => scope.Send(new CommitBookImportCommand(Input(
            year, Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["40"], "20")))));

        await AsSuperAdminAsync(scope => scope.Send(new LockLedgerCommand(result.LedgerId)));

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new UndoBookImportCommand(result.BatchId));
            return 0;
        }));
    }

    [Fact]
    public async Task Only_the_super_admin_may_import()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.Admin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

            return await scope.Send(new PreviewBookImportCommand(Input(
                FreshYear(), Row("Klijent", "Radnik", ["8"]))));
        }));
    }

    [Fact]
    public async Task Prices_with_either_separator_are_read()
    {
        var seeded = await SeedAsync();
        var year = FreshYear();

        var plan = await AsSuperAdminAsync(scope => scope.Send(new PreviewBookImportCommand(Input(
            year,
            Row(seeded.Client, $"{seeded.First} {seeded.Last}", ["1.040,5"], "20,5", "33.5")))));

        Assert.Equal(0, plan.ErrorCount);
        Assert.Equal(1040.5m, plan.SheetHours);
    }
}
