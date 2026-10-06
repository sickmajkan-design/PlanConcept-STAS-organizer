using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.FuelTransactions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The DKV statement check: a statement is stored, each row is paired with the
/// entry a driver recorded, and anything that does not line up is left for the
/// office. Runs against PostgreSQL because the pairing is a set of queries
/// across fuel entries, cards and earlier uploads.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class DkvImportTests : IntegrationTestBase
{
    public DkvImportTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private const string Header =
        "Št. kartice/kataloga;Registrska številka vozila;Čas transakcije;Status računa;Stroškovna skupina;"
        + "Skupna vrednost bruto (valuta računa);Skupina izdelkov;Vrsta izdelka;Koda izdelka;Država storitve";

    private static string NewCard() => "7043" + Guid.NewGuid().ToString("N")[..14];

    /// <summary>One statement line in DKV's column order for this test file.</summary>
    private static string Line(
        string card,
        string label = "SD SMART",
        string at = "01.09.2026 - 06:37",
        string status = "Obračunano",
        string amount = "100.00 EUR",
        string group = "Dizelsko gorivo",
        string product = "Dizelsko gorivo",
        string code = "WA0009") =>
        $"{card};{label};{at};{status};Stroški goriva;{amount};{group};{product};{code};DE";

    private static (string Name, long Size, MemoryStream Content) Csv(params string[] lines)
    {
        var bytes = Encoding.UTF8.GetBytes(string.Join("\r\n", new[] { Header }.Concat(lines)));

        return ("statement.csv", bytes.Length, new MemoryStream(bytes));
    }

    private static ImportDkvStatementCommand Import(params string[] lines)
    {
        var (name, size, content) = Csv(lines);

        return new ImportDkvStatementCommand { FileName = name, SizeBytes = size, Content = content };
    }

    private static PreviewDkvImportCommand Preview(params string[] lines)
    {
        var (name, size, content) = Csv(lines);

        return new PreviewDkvImportCommand { FileName = name, SizeBytes = size, Content = content };
    }

    private static void SignInAsAdmin(TestScope scope, User admin) =>
        scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

    private async Task<(User Admin, Vehicle Vehicle, string Card)> SeedFleetAsync(string? td = null)
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var card = NewCard();

        var vehicle = await InScope(async scope =>
        {
            var v = await TestData.SeedVehicleAsync(scope);
            v.TdNumber = td;
            scope.Db.FuelCards.Add(new FuelCard { VehicleId = v.Id, Provider = "DKV", CardNumber = card });
            await scope.Db.SaveChangesAsync();
            return v;
        });

        return (admin, vehicle, card);
    }

    private Task<VehicleExpense> DriverRecordsAsync(
        Guid vehicleId, DateOnly on, decimal amount, bool withReceipt = true) =>
        InScope(async scope =>
        {
            var expense = new VehicleExpense
            {
                VehicleId = vehicleId,
                Kind = VehicleExpenseKind.Fuel,
                Amount = amount,
                OccurredOn = on,
                Litres = 60m,
                OdometerKm = 150_000,
            };

            scope.Db.VehicleExpenses.Add(expense);
            await scope.Db.SaveChangesAsync();

            if (withReceipt)
            {
                scope.Db.Attachments.Add(new Attachment
                {
                    VehicleExpenseId = expense.Id,
                    FileName = "racun.jpg",
                    ContentType = "image/jpeg",
                    SizeBytes = 2048,
                    StorageKey = $"vehicle-expenses/{expense.Id}/racun.jpg",
                    Category = AttachmentCategory.Photo
                });
                await scope.Db.SaveChangesAsync();
            }

            return expense;
        });

    private Task<DkvImportResultDto> RunImportAsync(User admin, params string[] lines) =>
        InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(Import(lines));
        });

    private Task<List<FuelTransaction>> RowsOfAsync(string card) =>
        InScope(scope => scope.Db.FuelTransactions
            .AsNoTracking()
            .Where(t => t.CardNumber == card)
            .OrderBy(t => t.OccurredAtTime)
            .ToListAsync());

    [Fact]
    public async Task A_row_the_driver_also_recorded_is_matched_and_keeps_the_drivers_litres()
    {
        var (admin, vehicle, card) = await SeedFleetAsync();
        var expense = await DriverRecordsAsync(vehicle.Id, new DateOnly(2026, 9, 1), 100m);

        var result = await RunImportAsync(admin, Line(card));

        Assert.Equal(1, result.NewCount);
        var row = Assert.Single(await RowsOfAsync(card));
        Assert.Equal(FuelTransactionStatus.Matched, row.Status);
        Assert.Equal(expense.Id, row.VehicleExpenseId);
        Assert.Equal(vehicle.Id, row.VehicleId);

        // The driver's entry is untouched: litres are theirs, not the statement's.
        var stored = await InScope(scope => scope.Db.VehicleExpenses.AsNoTracking().FirstAsync(e => e.Id == expense.Id));
        Assert.Equal(60m, stored.Litres);
        Assert.Equal(100m, stored.Amount);
    }

    [Fact]
    public async Task An_entry_without_a_receipt_photo_is_flagged_until_the_photo_is_added()
    {
        var (admin, vehicle, card) = await SeedFleetAsync();
        var expense = await DriverRecordsAsync(vehicle.Id, new DateOnly(2026, 9, 1), 100m, withReceipt: false);

        await RunImportAsync(admin, Line(card));

        var flagged = Assert.Single(await RowsOfAsync(card));
        Assert.Equal(FuelTransactionStatus.NeedsReview, flagged.Status);
        Assert.Equal(FuelTransactionIssue.IncompleteEntry, flagged.Issue);
        Assert.Equal(expense.Id, flagged.VehicleExpenseId);

        await InScope(async scope =>
        {
            scope.Db.Attachments.Add(new Attachment
            {
                VehicleExpenseId = expense.Id,
                FileName = "racun.jpg",
                ContentType = "image/jpeg",
                SizeBytes = 2048,
                StorageKey = $"vehicle-expenses/{expense.Id}/racun.jpg",
                Category = AttachmentCategory.Photo
            });
            await scope.Db.SaveChangesAsync();
        });

        await InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(new RecheckDkvTransactionsCommand());
        });

        Assert.Equal(FuelTransactionStatus.Matched, Assert.Single(await RowsOfAsync(card)).Status);
    }

    [Fact]
    public async Task Importing_the_same_statement_twice_adds_nothing()
    {
        var (admin, _, card) = await SeedFleetAsync();
        var line = Line(card);

        await RunImportAsync(admin, line);
        var second = await RunImportAsync(admin, line);

        Assert.Equal(0, second.NewCount);
        Assert.Equal(1, second.DuplicateCount);
        Assert.Single(await RowsOfAsync(card));
    }

    [Fact]
    public async Task A_preview_writes_nothing()
    {
        var (admin, _, card) = await SeedFleetAsync();

        var preview = await InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(Preview(Line(card)));
        });

        Assert.Equal(1, preview.NewCount);
        Assert.Equal(1, preview.NoDriverEntryCount);
        Assert.Empty(await RowsOfAsync(card));
    }

    [Fact]
    public async Task A_row_that_later_turns_invoiced_updates_in_place()
    {
        var (admin, _, card) = await SeedFleetAsync();

        await RunImportAsync(admin, Line(card, status: "Ni obračunano"));
        var second = await RunImportAsync(admin, Line(card, status: "Obračunano"));

        Assert.Equal(1, second.UpdatedCount);
        var row = Assert.Single(await RowsOfAsync(card));
        Assert.True(row.IsInvoiced);
    }

    [Fact]
    public async Task Two_products_billed_in_the_same_minute_are_two_rows()
    {
        var (admin, _, card) = await SeedFleetAsync();

        var result = await RunImportAsync(admin,
            Line(card, amount: "129.91 EUR"),
            Line(card, amount: "18.02 EUR", group: "AdBlue", product: "AdBlue", code: "WA0019"));

        Assert.Equal(2, result.NewCount);
    }

    [Fact]
    public async Task A_row_nobody_recorded_is_matched_later_once_the_driver_catches_up()
    {
        var (admin, vehicle, card) = await SeedFleetAsync();
        await RunImportAsync(admin, Line(card));
        Assert.Equal(FuelTransactionStatus.NoDriverEntry, Assert.Single(await RowsOfAsync(card)).Status);

        await DriverRecordsAsync(vehicle.Id, new DateOnly(2026, 9, 1), 100m);

        await InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(new RecheckDkvTransactionsCommand());
        });

        Assert.Equal(FuelTransactionStatus.Matched, Assert.Single(await RowsOfAsync(card)).Status);
    }

    [Fact]
    public async Task An_unknown_card_is_matched_as_soon_as_it_is_assigned_to_a_vehicle()
    {
        var (admin, vehicle, _) = await SeedFleetAsync();
        var stranger = NewCard();
        await DriverRecordsAsync(vehicle.Id, new DateOnly(2026, 9, 1), 100m);

        await RunImportAsync(admin, Line(stranger));
        Assert.Equal(FuelTransactionStatus.UnknownCard, Assert.Single(await RowsOfAsync(stranger)).Status);

        await InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(new AssignDkvCardCommand { CardNumber = stranger, VehicleId = vehicle.Id });
        });

        Assert.Equal(FuelTransactionStatus.Matched, Assert.Single(await RowsOfAsync(stranger)).Status);
    }

    [Fact]
    public async Task A_td_belonging_to_another_vehicle_is_flagged_and_can_be_confirmed_with_a_reason()
    {
        var (admin, vehicle, card) = await SeedFleetAsync(td: "TD-OWN-" + Guid.NewGuid().ToString("N")[..6]);
        var other = "99" + Guid.NewGuid().ToString("N")[..5];
        await InScope(async scope =>
        {
            var o = await TestData.SeedVehicleAsync(scope);
            o.TdNumber = other;
            await scope.Db.SaveChangesAsync();
        });
        await DriverRecordsAsync(vehicle.Id, new DateOnly(2026, 9, 1), 100m);

        await RunImportAsync(admin, Line(card, label: other));

        var row = Assert.Single(await RowsOfAsync(card));
        Assert.Equal(FuelTransactionStatus.NeedsReview, row.Status);
        Assert.Equal(FuelTransactionIssue.TdMismatch, row.Issue);

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(new ResolveFuelTransactionCommand
            {
                Id = row.Id,
                Resolution = FuelTransactionResolution.Confirm
            });
        }));

        var resolved = await InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(new ResolveFuelTransactionCommand
            {
                Id = row.Id,
                Resolution = FuelTransactionResolution.Confirm,
                Note = "Card was lent to the neighbouring site."
            });
        });

        Assert.Equal("Resolved", resolved.Status);
        Assert.Equal("Card was lent to the neighbouring site.", resolved.ResolutionNote);
        Assert.NotNull(resolved.ResolvedAt);
    }

    [Fact]
    public async Task A_row_with_no_driver_entry_can_be_recorded_from_the_statement()
    {
        var (admin, vehicle, card) = await SeedFleetAsync();
        await RunImportAsync(admin, Line(card, amount: "77.70 EUR"));
        var row = Assert.Single(await RowsOfAsync(card));

        await InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(new ResolveFuelTransactionCommand
            {
                Id = row.Id,
                Resolution = FuelTransactionResolution.CreateExpense,
                Litres = 55.5m,
                OdometerKm = 120_000
            });
        });

        var expenses = await InScope(scope => scope.Db.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.VehicleId == vehicle.Id && e.Kind == VehicleExpenseKind.Fuel)
            .ToListAsync());

        var created = Assert.Single(expenses);
        Assert.Equal(77.70m, created.Amount);
        Assert.Equal("DKV", created.Supplier);
        Assert.Equal(VehicleExpenseStatus.Pending, created.Status);
        Assert.Equal(55.5m, created.Litres);
        Assert.Equal(120_000, created.OdometerKm);
        Assert.Equal(created.Id, (await RowsOfAsync(card)).Single().VehicleExpenseId);
    }

    [Fact]
    public async Task One_driver_entry_cannot_be_paired_with_two_rows()
    {
        var (admin, vehicle, card) = await SeedFleetAsync();
        var expense = await DriverRecordsAsync(vehicle.Id, new DateOnly(2026, 9, 1), 100m);
        await RunImportAsync(admin,
            Line(card, at: "01.09.2026 - 06:37"),
            Line(card, at: "01.09.2026 - 06:40"));

        var rows = await RowsOfAsync(card);

        Assert.Equal(1, rows.Count(r => r.VehicleExpenseId == expense.Id));
        Assert.Equal(1, rows.Count(r => r.Status == FuelTransactionStatus.NoDriverEntry));
    }

    [Fact]
    public async Task Only_admins_may_import_a_statement()
    {
        var (_, _, card) = await SeedFleetAsync();
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            scope.CurrentUser.SignInAs(foreman.Id, foreman.Role, null, foreman.Email);
            return scope.Send(Import(Line(card)));
        }));
    }

    [Fact]
    public async Task A_file_that_is_not_a_dkv_statement_is_refused_and_nothing_is_stored()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var bytes = Encoding.UTF8.GetBytes("Name;Amount\r\na;1");

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            SignInAsAdmin(scope, admin);
            return scope.Send(new ImportDkvStatementCommand
            {
                FileName = "x.csv",
                SizeBytes = bytes.Length,
                Content = new MemoryStream(bytes)
            });
        }));
    }
}
