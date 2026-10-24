using System.Globalization;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.WeeklySiteReports.Commands.CreateWeeklySiteReport;
using Construction.Application.Features.WeeklySiteReports.Commands.MarkWeeklySiteReportProcessed;
using Construction.Application.Features.WeeklySiteReports.Commands.RequestWeeklySiteReportCorrection;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReport;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A report is a list of workers with hours and several pictures. Approving it enters those hours into the month's
/// sheet, week by week and worker by worker; nothing is entered before the approval, a cell that says something else
/// can be kept, and taking the approval back takes the hours back.
/// </summary>
/// <remarks>
/// Each test uses a year of its own, so the sheets one test makes are never the ones another test's report finds.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class WeeklyReportLedgerEntryTests : IntegrationTestBase
{
    public WeeklyReportLedgerEntryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private sealed record World(Project Project, Employee Anna, Employee Boris, User Office, User Super);

    private async Task<World> SeedAsync()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, $"Aldi {Guid.NewGuid():N}"[..14]));
        var anna = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Anna", lastName: "Kovač"));
        var boris = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Boris", lastName: "Ilić"));
        var office = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var super = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        return new World(project, anna, boris, office, super);
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static ReportFileInput Picture(string name = "satnica.jpg") =>
        new(name, 13, new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0xFF, 0xD9]));

    /// <summary>Week 10 of the year: a Monday to Sunday that stays inside March.</summary>
    private const int WholeWeek = 10;

    private static int WholeWeekColumn(int year) =>
        LedgerTemplates.MonthWeeks(year, 3).ToList().FindIndex(w => w.IsoWeek == WholeWeek) + 1;

    private Task<Ledger> MakeLedgerAsync(World w, int year, int month) =>
        InScope(async scope =>
        {
            ActAs(scope, w.Super);
            var dto = await scope.Send(new CreateLedgerCommand
            {
                Name = $"Obračun {year}-{month} {Guid.NewGuid():N}"[..24],
                Year = year,
                Month = month,
                Template = LedgerTemplates.Payroll,
            });

            return await scope.Db.Ledgers.AsNoTracking().SingleAsync(l => l.Id == dto.Id);
        });

    private Task<WeeklySiteReportDetailDto> SendAsync(
        World w, int year, int week, IReadOnlyList<ReportLineInput> lines, int pictures = 1) =>
        InScope(async scope =>
        {
            ActAs(scope, w.Office);
            var created = await scope.Send(new CreateWeeklySiteReportCommand
            {
                ProjectId = w.Project.Id,
                IsoYear = year,
                IsoWeek = week,
                Type = WeeklyReportType.SignedHours,
                SubmittedByEmployeeId = w.Anna.Id,
                Files = Enumerable.Range(1, pictures).Select(i => Picture($"strana{i}.jpg")).ToList(),
                Lines = lines,
            });

            return await scope.Send(new GetWeeklySiteReportQuery(created.Id));
        });

    private Task<ApproveWeeklySiteReportResult> ApproveAsync(
        World w, Guid reportId, IReadOnlyList<ReportLineInput>? lines = null, IReadOnlyList<Guid>? keep = null) =>
        InScope(scope =>
        {
            ActAs(scope, w.Office);
            return scope.Send(new MarkWeeklySiteReportProcessedCommand(reportId, lines, keep));
        });

    private Task<string?> CellAsync(Guid ledgerId, Guid employeeId, int weekColumn) =>
        InScope(scope => scope.Db.LedgerCells
            .Where(c => c.Row.Section.LedgerId == ledgerId
                && c.Row.EmployeeId == employeeId
                && c.Column.SystemKey == LedgerTemplates.Keys.Week(weekColumn))
            .Select(c => c.Value)
            .FirstOrDefaultAsync());

    private static string Hours(decimal h) => h.ToString("0.##", CultureInfo.InvariantCulture);

    [Fact]
    public async Task A_report_keeps_all_its_pictures_and_every_worker_and_totals_their_hours()
    {
        var w = await SeedAsync();

        var report = await SendAsync(
            w, 2031, WholeWeek,
            [new ReportLineInput(w.Anna.Id, 45m), new ReportLineInput(w.Boris.Id, 42.5m)],
            pictures: 2);

        Assert.Equal(2, report.Pages.Count);
        Assert.Equal(2, report.Lines.Count);
        Assert.Equal(87.5m, report.Quantity);
        Assert.Equal(WeeklyReportStatus.Submitted, report.Status);
        Assert.Contains(report.Lines, l => l.EmployeeName == "Anna Kovač" && l.Hours == 45m);
    }

    [Fact]
    public async Task A_second_submission_for_the_same_site_and_week_joins_the_first_one()
    {
        var w = await SeedAsync();
        await SendAsync(w, 2032, WholeWeek, [new ReportLineInput(w.Anna.Id, 40m)], pictures: 2);

        var merged = await SendAsync(w, 2032, WholeWeek, [new ReportLineInput(w.Boris.Id, 38m)]);

        Assert.Equal(3, merged.Pages.Count);
        Assert.Equal(2, merged.Lines.Count);
        Assert.Equal(78m, merged.Quantity);
        var count = await InScope(scope => scope.Db.WeeklySiteReports.CountAsync(r => r.ProjectId == w.Project.Id));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Nothing_is_entered_in_the_sheet_before_the_approval()
    {
        var w = await SeedAsync();
        var ledger = await MakeLedgerAsync(w, 2033, 3);

        await SendAsync(w, 2033, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m)]);

        Assert.Null(await CellAsync(ledger.Id, w.Anna.Id, WholeWeekColumn(2033)));
    }

    [Fact]
    public async Task Approving_enters_each_worker_in_the_right_row_and_week_and_makes_the_section_and_rows_as_needed()
    {
        var w = await SeedAsync();
        var ledger = await MakeLedgerAsync(w, 2034, 3);
        var report = await SendAsync(
            w, 2034, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m), new ReportLineInput(w.Boris.Id, 42.5m)]);

        var result = await ApproveAsync(w, report.Id);

        Assert.Equal(2, result.CellsWritten);
        Assert.Equal("45", await CellAsync(ledger.Id, w.Anna.Id, WholeWeekColumn(2034)));
        Assert.Equal("42.5", await CellAsync(ledger.Id, w.Boris.Id, WholeWeekColumn(2034)));

        var marked = await InScope(scope => scope.Db.LedgerCells.CountAsync(c => c.SourceReportId == report.Id));
        Assert.Equal(2, marked);
    }

    [Fact]
    public async Task The_office_can_correct_the_hours_while_approving_and_the_correction_is_what_is_entered()
    {
        var w = await SeedAsync();
        var ledger = await MakeLedgerAsync(w, 2035, 3);
        var report = await SendAsync(w, 2035, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m)]);

        await ApproveAsync(w, report.Id, [new ReportLineInput(w.Anna.Id, 40m), new ReportLineInput(w.Boris.Id, 8m)]);

        Assert.Equal("40", await CellAsync(ledger.Id, w.Anna.Id, WholeWeekColumn(2035)));
        Assert.Equal("8", await CellAsync(ledger.Id, w.Boris.Id, WholeWeekColumn(2035)));
        var stored = await InScope(scope => scope.Db.WeeklySiteReports.SingleAsync(r => r.Id == report.Id));
        Assert.Equal(48m, stored.Quantity);
    }

    [Fact]
    public async Task A_cell_that_says_something_else_is_replaced_unless_the_worker_is_kept()
    {
        var w = await SeedAsync();
        var ledger = await MakeLedgerAsync(w, 2036, 3);
        var first = await SendAsync(w, 2036, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m)]);
        await ApproveAsync(w, first.Id);

        // Somebody types over it, then a new report for the same week arrives and is approved.
        var rowId = await InScope(scope => scope.Db.LedgerRows
            .Where(r => r.Section.LedgerId == ledger.Id && r.EmployeeId == w.Anna.Id)
            .Select(r => r.Id)
            .SingleAsync());
        var weekKey = LedgerTemplates.Keys.Week(WholeWeekColumn(2036));
        var columnId = await InScope(scope => scope.Db.LedgerColumns
            .Where(c => c.LedgerId == ledger.Id && c.SystemKey == weekKey)
            .Select(c => c.Id)
            .SingleAsync());

        await InScope(scope =>
        {
            ActAs(scope, w.Super);
            return scope.Send(new SetLedgerCellCommand { RowId = rowId, ColumnId = columnId, Value = "30" });
        });

        var again = await SendAsync(w, 2036, WholeWeek, [new ReportLineInput(w.Boris.Id, 1m)]);

        var keptResult = await ApproveAsync(w, again.Id, [new ReportLineInput(w.Anna.Id, 45m)], keep: [w.Anna.Id]);
        Assert.Equal(1, keptResult.CellsKept);
        Assert.Equal("30", await CellAsync(ledger.Id, w.Anna.Id, WholeWeekColumn(2036)));

        var replaced = await ApproveAsync(w, again.Id, [new ReportLineInput(w.Anna.Id, 45m)]);
        Assert.Equal(1, replaced.CellsWritten);
        Assert.Equal("45", await CellAsync(ledger.Id, w.Anna.Id, WholeWeekColumn(2036)));
    }

    [Fact]
    public async Task A_week_that_runs_over_a_month_is_split_between_the_two_sheets()
    {
        var w = await SeedAsync();

        // The first ISO week of 2040 whose Monday and Sunday are in different months.
        var (year, week) = Enumerable.Range(1, 52)
            .Select(n => (Year: 2040, Week: n))
            .First(x =>
            {
                var monday = System.Globalization.ISOWeek.ToDateTime(x.Year, x.Week, DayOfWeek.Monday);
                return monday.AddDays(6).Month != monday.Month;
            });

        var mondayDate = System.Globalization.ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var sundayDate = mondayDate.AddDays(6);
        var firstLedger = await MakeLedgerAsync(w, mondayDate.Year, mondayDate.Month);
        var secondLedger = await MakeLedgerAsync(w, sundayDate.Year, sundayDate.Month);

        var report = await SendAsync(w, year, week, [new ReportLineInput(w.Anna.Id, 40m, 16m)]);
        await ApproveAsync(w, report.Id);

        int ColumnIn(int y, int m) =>
            LedgerTemplates.MonthWeeks(y, m).ToList().FindIndex(x => x.IsoYear == year && x.IsoWeek == week) + 1;

        Assert.Equal("24", await CellAsync(firstLedger.Id, w.Anna.Id, ColumnIn(mondayDate.Year, mondayDate.Month)));
        Assert.Equal("16", await CellAsync(secondLedger.Id, w.Anna.Id, ColumnIn(sundayDate.Year, sundayDate.Month)));
    }

    [Fact]
    public async Task Hours_cannot_be_put_in_the_next_month_for_a_week_that_stays_in_one()
    {
        var w = await SeedAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            SendAsync(w, 2037, WholeWeek, [new ReportLineInput(w.Anna.Id, 40m, 8m)]));
    }

    [Fact]
    public async Task The_same_worker_twice_is_refused()
    {
        var w = await SeedAsync();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            SendAsync(w, 2038, WholeWeek, [new ReportLineInput(w.Anna.Id, 20m), new ReportLineInput(w.Anna.Id, 20m)]));
    }

    [Fact]
    public async Task A_month_with_no_sheet_stops_the_approval_and_leaves_the_report_waiting()
    {
        var w = await SeedAsync();
        var report = await SendAsync(w, 2039, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m)]);

        var error = await Assert.ThrowsAsync<ConflictException>(() => ApproveAsync(w, report.Id));

        Assert.Contains("no monthly sheet", error.Message);
        var stored = await InScope(scope => scope.Db.WeeklySiteReports.SingleAsync(r => r.Id == report.Id));
        Assert.Equal(WeeklyReportStatus.Submitted, stored.Status);
    }

    [Fact]
    public async Task Sending_an_approved_report_back_takes_its_hours_out_of_the_sheet()
    {
        var w = await SeedAsync();
        var ledger = await MakeLedgerAsync(w, 2041, 3);
        var report = await SendAsync(w, 2041, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m)]);
        await ApproveAsync(w, report.Id);
        Assert.Equal("45", await CellAsync(ledger.Id, w.Anna.Id, WholeWeekColumn(2041)));

        await InScope(scope =>
        {
            ActAs(scope, w.Office);
            return scope.Send(new RequestWeeklySiteReportCorrectionCommand(report.Id, "Pogrešni sati"));
        });

        Assert.True(string.IsNullOrEmpty(await CellAsync(ledger.Id, w.Anna.Id, WholeWeekColumn(2041))));
    }

    [Fact]
    public async Task The_detail_says_what_the_sheet_holds_now_for_each_worker_and_whether_there_is_a_sheet()
    {
        var w = await SeedAsync();
        var none = await SendAsync(w, 2042, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m)]);
        Assert.False(none.FirstMonth.LedgerFound);

        var ledger = await MakeLedgerAsync(w, 2043, 3);
        var report = await SendAsync(w, 2043, WholeWeek, [new ReportLineInput(w.Anna.Id, 45m)]);
        Assert.True(report.FirstMonth.LedgerFound);
        Assert.Null(report.Lines.Single().ExistingFirstMonth);

        await ApproveAsync(w, report.Id);
        var after = await InScope(scope =>
        {
            ActAs(scope, w.Office);
            return scope.Send(new GetWeeklySiteReportQuery(report.Id));
        });

        Assert.Equal(45m, after.Lines.Single().ExistingFirstMonth);
        Assert.Equal(ledger.Name, after.FirstMonth.LedgerName);
    }
}
