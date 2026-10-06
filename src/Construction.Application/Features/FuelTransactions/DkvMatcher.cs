using System.Globalization;
using Construction.Domain.Enums;

namespace Construction.Application.Features.FuelTransactions;

/// <summary>The vehicle a fuel card is issued to, as far as matching cares.</summary>
public record DkvVehicleInfo(
    Guid VehicleId,
    string Name,
    string? TdNumber,
    string RegistrationNumber,
    FuelType FuelType);

/// <summary>A fuel entry a driver recorded, available to be paired with a statement row.</summary>
public record DkvExpenseInfo(
    Guid Id,
    Guid VehicleId,
    DateOnly OccurredOn,
    decimal Amount,
    bool HasOdometer,
    bool HasReceipt);

public record DkvMatch(
    Guid? VehicleId,
    Guid? ExpenseId,
    FuelTransactionStatus Status,
    FuelTransactionIssue Issue,
    string? Detail);

/// <summary>
/// Decides, for each statement row, which vehicle it belongs to and which
/// driver entry it corresponds to. Pure and database-free, so the preview and
/// the real import cannot disagree.
/// </summary>
/// <remarks>
/// The driver's entry is the record of what happened (litres, odometer,
/// receipt); the statement only confirms it. So a row is "matched" when a
/// driver recorded the same amount for that vehicle on that day (or the day
/// either side, since a fill-up at 00:15 is often entered under the evening
/// before), and everything else is surfaced for a person to look at.
/// </remarks>
public static class DkvMatcher
{
    /// <summary>Two amounts closer than this are the same amount (rounding on the receipt).</summary>
    public const decimal AmountTolerance = 0.01m;

    public static List<DkvMatch> Match(
        IReadOnlyList<DkvRow> rows,
        IReadOnlyDictionary<string, DkvVehicleInfo> vehicleByCard,
        IEnumerable<DkvExpenseInfo> availableExpenses,
        IReadOnlySet<string> knownVehicleLabels)
    {
        var results = new DkvMatch?[rows.Count];
        var claimed = new HashSet<Guid>();
        var expenseList = availableExpenses.ToList();
        var expensesById = expenseList.ToDictionary(e => e.Id);
        var expensesByVehicle = expenseList
            .GroupBy(e => e.VehicleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var vehicles = new DkvVehicleInfo?[rows.Count];
        var pairedExpense = new Guid?[rows.Count];
        var issueOf = new (FuelTransactionIssue Issue, string? Detail)[rows.Count];

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            if (!vehicleByCard.TryGetValue(row.CardNumber.ToLowerInvariant(), out var vehicle))
            {
                results[i] = new DkvMatch(
                    null, null, FuelTransactionStatus.UnknownCard, FuelTransactionIssue.UnknownCard,
                    $"No vehicle has card {row.CardNumber}.");
                continue;
            }

            vehicles[i] = vehicle;
            issueOf[i] = VehicleIssue(row, vehicle, knownVehicleLabels);
        }

        // Pass 1: the same amount. Same day wins over a neighbouring day.
        for (var i = 0; i < rows.Count; i++)
        {
            if (vehicles[i] is not { } vehicle
                || !expensesByVehicle.TryGetValue(vehicle.VehicleId, out var candidates))
            {
                continue;
            }

            var row = rows[i];

            var exact = candidates
                .Where(e => !claimed.Contains(e.Id)
                    && Math.Abs(e.Amount - row.Amount) <= AmountTolerance
                    && Math.Abs(e.OccurredOn.DayNumber - row.Date.DayNumber) <= 1)
                .OrderBy(e => Math.Abs(e.OccurredOn.DayNumber - row.Date.DayNumber))
                .FirstOrDefault();

            if (exact is not null)
            {
                claimed.Add(exact.Id);
                pairedExpense[i] = exact.Id;
            }
        }

        // Pass 2: nothing at that amount, but a driver entry on that very day.
        for (var i = 0; i < rows.Count; i++)
        {
            if (vehicles[i] is not { } vehicle || pairedExpense[i] is not null
                || !expensesByVehicle.TryGetValue(vehicle.VehicleId, out var candidates))
            {
                continue;
            }

            var row = rows[i];

            var near = candidates
                .Where(e => !claimed.Contains(e.Id) && e.OccurredOn == row.Date)
                .OrderBy(e => Math.Abs(e.Amount - row.Amount))
                .FirstOrDefault();

            if (near is not null)
            {
                claimed.Add(near.Id);
                pairedExpense[i] = near.Id;
                if (issueOf[i].Issue == FuelTransactionIssue.None)
                {
                    issueOf[i] = (FuelTransactionIssue.AmountMismatch,
                        string.Create(CultureInfo.InvariantCulture,
                            $"Statement {row.Amount:0.00} {row.Currency}, driver recorded {near.Amount:0.00}."));
                }
            }
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (results[i] is not null)
            {
                continue;
            }

            var vehicle = vehicles[i]!;
            var (issue, detail) = issueOf[i];

            if (issue != FuelTransactionIssue.None)
            {
                results[i] = new DkvMatch(
                    vehicle.VehicleId, pairedExpense[i], FuelTransactionStatus.NeedsReview, issue, detail);
            }
            else if (pairedExpense[i] is { } expenseId)
            {
                // Same vehicle, day and amount: it agrees. But a fill-up is only a record worth
                // keeping with the odometer reading and the receipt behind it.
                var missing = MissingParts(expensesById[expenseId]);

                results[i] = missing is null
                    ? new DkvMatch(
                        vehicle.VehicleId, expenseId, FuelTransactionStatus.Matched, FuelTransactionIssue.None, null)
                    : new DkvMatch(
                        vehicle.VehicleId, expenseId, FuelTransactionStatus.NeedsReview,
                        FuelTransactionIssue.IncompleteEntry, $"The driver's entry has no {missing}.");
            }
            else
            {
                results[i] = new DkvMatch(
                    vehicle.VehicleId, null, FuelTransactionStatus.NoDriverEntry,
                    FuelTransactionIssue.NoDriverEntry, "No driver has recorded this fill-up.");
            }
        }

        return results.Select(r => r!).ToList();
    }

    private static string? MissingParts(DkvExpenseInfo expense) => (expense.HasOdometer, expense.HasReceipt) switch
    {
        (true, true) => null,
        (false, true) => "odometer reading",
        (true, false) => "receipt photo",
        _ => "odometer reading or receipt photo"
    };

    private static (FuelTransactionIssue, string?) VehicleIssue(
        DkvRow row, DkvVehicleInfo vehicle, IReadOnlySet<string> knownVehicleLabels)
    {
        // The registration column is a free label on a card: usually the
        // vehicle's TD, but a fleet-wide label ("SD SMART") on many cards.
        // Only a value that looks like a vehicle number is a claim worth checking.
        var claim = row.VehicleLabel?.Trim();

        if (!string.IsNullOrEmpty(claim) && LooksLikeVehicleNumber(claim, knownVehicleLabels))
        {
            var ownTd = vehicle.TdNumber;
            var matchesTd = ownTd is not null && string.Equals(ownTd, claim, StringComparison.OrdinalIgnoreCase);
            var matchesPlate = string.Equals(vehicle.RegistrationNumber, claim, StringComparison.OrdinalIgnoreCase);

            if (!matchesTd && !matchesPlate)
            {
                return (FuelTransactionIssue.TdMismatch, ownTd is null
                    ? $"Statement says {claim}, but {vehicle.Name} has no TD number yet."
                    : $"Statement says {claim}, but the card is issued to {vehicle.Name} (TD {ownTd}).");
            }
        }

        var kind = ProductKind(row);

        if (kind is ProductKinds.Diesel or ProductKinds.Petrol
            && vehicle.FuelType != FuelType.Hybrid
            && !FuelAgrees(vehicle.FuelType, kind))
        {
            return (FuelTransactionIssue.FuelTypeMismatch,
                $"{row.ProductType ?? row.ProductGroup} bought on {vehicle.Name}, which runs on {vehicle.FuelType}.");
        }

        return (FuelTransactionIssue.None, null);
    }

    private static bool LooksLikeVehicleNumber(string claim, IReadOnlySet<string> knownLabels) =>
        claim.All(char.IsDigit) || knownLabels.Contains(claim.ToLowerInvariant());

    private static bool FuelAgrees(FuelType fuelType, ProductKinds kind) => (fuelType, kind) switch
    {
        (FuelType.Diesel, ProductKinds.Diesel) => true,
        (FuelType.Petrol, ProductKinds.Petrol) => true,
        _ => false
    };

    internal enum ProductKinds
    {
        Other,
        Diesel,
        Petrol,
        AdBlue
    }

    internal static ProductKinds ProductKind(DkvRow row)
    {
        var text = DkvStatementParser.Normalise($"{row.ProductGroup} {row.ProductType}");

        if (text.Contains("adblue", StringComparison.Ordinal))
        {
            return ProductKinds.AdBlue;
        }

        if (text.Contains("dizel", StringComparison.Ordinal) || text.Contains("diesel", StringComparison.Ordinal))
        {
            return ProductKinds.Diesel;
        }

        if (text.Contains("bencin", StringComparison.Ordinal) || text.Contains("benzin", StringComparison.Ordinal)
            || text.Contains("petrol", StringComparison.Ordinal))
        {
            return ProductKinds.Petrol;
        }

        return ProductKinds.Other;
    }
}
