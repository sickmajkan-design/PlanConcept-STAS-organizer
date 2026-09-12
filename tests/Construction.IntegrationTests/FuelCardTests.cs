using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.FuelCards.Commands;
using Construction.Application.Features.FuelCards.Import;
using Construction.Application.Features.FuelCards.Queries;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The fuel-card registry and the statement-import mechanism it backs: the
/// company reads DKV's (or any other provider's) monthly export, maps its
/// columns once, and the app turns matched rows into vehicle fuel expenses.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class FuelCardTests : IntegrationTestBase
{
    public FuelCardTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static readonly FuelImportColumnMapping Mapping = new()
    {
        CardNumberColumn = 0,
        OccurredOnColumn = 1,
        AmountColumn = 2,
        LitresColumn = 3,
    };

    private static MemoryStream CsvStream(string csv) =>
        new(Encoding.UTF8.GetBytes(csv));

    private async Task<(Vehicle Vehicle, User Foreman)> SeedFleetKeeperAsync()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        return (vehicle, foreman);
    }

    [Fact]
    public async Task Adding_a_fuel_card_registers_it_against_the_vehicle()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();

        var card = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new AddFuelCardCommand
            {
                VehicleId = vehicle.Id,
                Provider = "DKV",
                CardNumber = $"DKV-{Guid.NewGuid():N}",
            });
        });

        Assert.Equal(vehicle.Id, card.VehicleId);
        Assert.Equal("DKV", card.Provider);
    }

    [Fact]
    public async Task A_second_card_with_the_same_number_is_refused()
    {
        // "izdaje se uz auto" — one card belongs to exactly one vehicle at a
        // time. Two live cards sharing a number would make the statement
        // import unable to tell which vehicle drank the fuel.
        var (vehicleA, foreman) = await SeedFleetKeeperAsync();
        var vehicleB = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var cardNumber = $"DKV-{Guid.NewGuid():N}";

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new AddFuelCardCommand
            {
                VehicleId = vehicleA.Id,
                Provider = "DKV",
                CardNumber = cardNumber,
            });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new AddFuelCardCommand
            {
                VehicleId = vehicleB.Id,
                Provider = "DKV",
                // Case shouldn't matter — the join key is compared case-insensitively.
                CardNumber = cardNumber.ToUpperInvariant(),
            });
        }));
    }

    [Fact]
    public async Task A_retired_cards_number_can_be_reused()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var cardNumber = $"DKV-{Guid.NewGuid():N}";

        var card = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new AddFuelCardCommand
            {
                VehicleId = vehicle.Id,
                Provider = "DKV",
                CardNumber = cardNumber,
            });
        });

        // Deleting a fuel card is narrower than recording one — matches
        // CostRules.CanDeleteSpending, which stops at Project Manager.
        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new DeleteFuelCardCommand(card.Id));
        });

        // Should not throw: the old card is soft-deleted, so its number is free.
        var replacement = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new AddFuelCardCommand
            {
                VehicleId = vehicle.Id,
                Provider = "DKV",
                CardNumber = cardNumber,
            });
        });

        Assert.NotEqual(card.Id, replacement.Id);
    }

    [Fact]
    public async Task Preview_matches_a_known_card_and_flags_the_rest()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var cardNumber = $"DKV-{Guid.NewGuid():N}";

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new AddFuelCardCommand
            {
                VehicleId = vehicle.Id,
                Provider = "DKV",
                CardNumber = cardNumber,
            });
        });

        var csv =
            "CardNumber,Date,Amount,Litres\n" +
            $"{cardNumber},01.03.2026,50.25,40.5\n" +
            "UNKNOWN-CARD-999,01.03.2026,60,45\n" +
            $"{cardNumber},02.03.2026,not-a-number,10\n";

        var preview = await InScope(scope =>
        {
            ActAs(scope, foreman);
            using var content = CsvStream(csv);
            return scope.Send(new PreviewFuelImportCommand
            {
                FileName = "statement.csv",
                SizeBytes = content.Length,
                Content = content,
                HasHeaderRow = true,
                Mapping = Mapping,
            });
        });

        Assert.Equal(3, preview.TotalRows);
        Assert.Equal(1, preview.ReadyCount);
        Assert.Equal(2, preview.ProblemCount);

        var matched = Assert.Single(preview.Rows, r => r.Status == FuelImportRowStatus.Ready);
        Assert.Equal(vehicle.Id, matched.VehicleId);

        var unmatched = Assert.Single(preview.Rows, r => r.Status == FuelImportRowStatus.NoMatchingCard);
        Assert.Equal("UNKNOWN-CARD-999", unmatched.CardNumber);

        Assert.Single(preview.Rows, r => r.Status == FuelImportRowStatus.InvalidAmount);
    }

    [Fact]
    public async Task Commit_creates_only_the_clean_rows_and_is_safe_to_repeat()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var cardNumber = $"DKV-{Guid.NewGuid():N}";

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new AddFuelCardCommand
            {
                VehicleId = vehicle.Id,
                Provider = "DKV",
                CardNumber = cardNumber,
            });
        });

        var csv =
            "CardNumber,Date,Amount,Litres\n" +
            $"{cardNumber},01.03.2026,50.25,40.5\n" +
            "UNKNOWN-CARD-999,01.03.2026,60,45\n";

        var firstResult = await InScope(scope =>
        {
            ActAs(scope, foreman);
            using var content = CsvStream(csv);
            return scope.Send(new ImportFuelTransactionsCommand
            {
                FileName = "statement.csv",
                SizeBytes = content.Length,
                Content = content,
                HasHeaderRow = true,
                Mapping = Mapping,
            });
        });

        Assert.Equal(1, firstResult.CreatedCount);
        Assert.Equal(1, firstResult.SkippedCount);

        var expenses = await InScope(scope => scope.Db.VehicleExpenses
            .Where(e => e.VehicleId == vehicle.Id && e.Kind == VehicleExpenseKind.Fuel)
            .ToListAsync());

        var created = Assert.Single(expenses);
        Assert.Equal(50.25m, created.Amount);
        Assert.Equal(40.5m, created.Litres);
        Assert.Contains("DKV", created.Note);
        Assert.Contains(cardNumber, created.Note);

        // Re-running the exact same statement — a real scenario at month
        // boundaries — must not double the cost.
        var secondResult = await InScope(scope =>
        {
            ActAs(scope, foreman);
            using var content = CsvStream(csv);
            return scope.Send(new ImportFuelTransactionsCommand
            {
                FileName = "statement.csv",
                SizeBytes = content.Length,
                Content = content,
                HasHeaderRow = true,
                Mapping = Mapping,
            });
        });

        Assert.Equal(0, secondResult.CreatedCount);

        var expensesAfterRerun = await InScope(scope => scope.Db.VehicleExpenses
            .Where(e => e.VehicleId == vehicle.Id && e.Kind == VehicleExpenseKind.Fuel)
            .CountAsync());

        Assert.Equal(1, expensesAfterRerun);
    }
}
