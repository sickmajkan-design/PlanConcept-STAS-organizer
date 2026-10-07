using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelTransactions;

/// <summary>One statement row and what the import decided about it.</summary>
internal class DkvPlanItem
{
    public DkvRow Row { get; init; } = null!;

    /// <summary>The row already on file, when this is not new.</summary>
    public FuelTransaction? Existing { get; init; }

    /// <summary>False for an unresolved row from an earlier upload that is being re-checked.</summary>
    public bool FromFile { get; init; }

    public DkvRowOutcome Outcome { get; init; }

    public DkvMatch Match { get; init; } = null!;

    /// <summary>Whether the stored status/issue/link should be rewritten.</summary>
    public bool RewritesMatch { get; init; }
}

internal class DkvPlan
{
    public List<DkvPlanItem> Items { get; init; } = [];

    public IReadOnlyDictionary<Guid, DkvVehicleInfo> Vehicles { get; init; } = new Dictionary<Guid, DkvVehicleInfo>();

    public IReadOnlyDictionary<string, DkvVehicleInfo> VehiclesByTd { get; init; } =
        new Dictionary<string, DkvVehicleInfo>();
}

/// <summary>
/// Works out what importing a statement would do, without writing. Both the
/// preview and the import run this, then the import applies the result.
/// </summary>
/// <remarks>
/// Unresolved rows from earlier uploads are matched again every time: a driver
/// who enters yesterday's receipt today turns an old "no driver entry" into a
/// match without anyone touching it.
/// </remarks>
internal static class DkvImportPlanner
{
    private static readonly FuelTransactionStatus[] Unresolved =
    [
        FuelTransactionStatus.NoDriverEntry,
        FuelTransactionStatus.NeedsReview,
        FuelTransactionStatus.UnknownCard
    ];

    public static async Task<DkvPlan> PlanAsync(
        IApplicationDbContext context,
        IReadOnlyList<DkvRow> rows,
        CancellationToken cancellationToken)
    {
        var fileCards = rows.Select(r => r.CardNumber).Distinct().ToList();

        var existingInFile = await context.FuelTransactions
            .Where(t => fileCards.Contains(t.CardNumber))
            .ToListAsync(cancellationToken);

        var existingByKey = existingInFile.ToDictionary(Key);

        var unresolved = await context.FuelTransactions
            .Where(t => Unresolved.Contains(t.Status))
            .ToListAsync(cancellationToken);

        var unresolvedIds = unresolved.Select(t => t.Id).ToHashSet();

        // What goes through the matcher: rows not seen before, plus everything
        // still open from earlier. Rows already settled keep what they have.
        var toMatch = new List<(DkvRow Row, FuelTransaction? Existing, bool FromFile, DkvRowOutcome Outcome)>();
        var seenKeys = new HashSet<string>();
        var settled = new List<DkvPlanItem>();

        foreach (var row in rows)
        {
            var key = Key(row);

            if (!seenKeys.Add(key))
            {
                continue;
            }

            if (!existingByKey.TryGetValue(key, out var existing))
            {
                toMatch.Add((row, null, true, DkvRowOutcome.New));
                continue;
            }

            var outcome = existing.IsInvoiced != row.IsInvoiced ? DkvRowOutcome.Updated : DkvRowOutcome.Duplicate;

            if (unresolvedIds.Contains(existing.Id))
            {
                toMatch.Add((row, existing, true, outcome));
            }
            else
            {
                settled.Add(new DkvPlanItem
                {
                    Row = row,
                    Existing = existing,
                    FromFile = true,
                    Outcome = outcome,
                    Match = new DkvMatch(
                        existing.VehicleId, existing.VehicleExpenseId, existing.Status, existing.Issue,
                        existing.IssueDetail),
                });
            }
        }

        foreach (var open in unresolved)
        {
            // Open rows that are not in this file at all are re-checked too.
            if (!seenKeys.Contains(Key(open)))
            {
                toMatch.Add((ToRow(open), open, false, DkvRowOutcome.Duplicate));
            }
        }

        var vehicleRows = await context.Vehicles
            .AsNoTracking()
            .Select(v => new
            {
                v.Id,
                Name = v.Brand + " " + v.Model + " (" + v.RegistrationNumber + ")",
                v.TdNumber,
                v.RegistrationNumber,
                v.FuelType
            })
            .ToListAsync(cancellationToken);

        var vehicles = vehicleRows.ToDictionary(
            v => v.Id,
            v => new DkvVehicleInfo(v.Id, v.Name, v.TdNumber, v.RegistrationNumber, v.FuelType));

        var knownLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var vehiclesByTd = new Dictionary<string, DkvVehicleInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in vehicles.Values)
        {
            knownLabels.Add(v.RegistrationNumber.ToLowerInvariant());

            if (v.TdNumber is not null)
            {
                knownLabels.Add(v.TdNumber.ToLowerInvariant());
                vehiclesByTd[v.TdNumber] = v;
            }
        }

        var matchCards = toMatch.Select(m => m.Row.CardNumber).Distinct().ToList();

        var cardRows = await context.FuelCards
            .AsNoTracking()
            .Where(c => matchCards.Contains(c.CardNumber))
            .Select(c => new { c.CardNumber, c.VehicleId })
            .ToListAsync(cancellationToken);

        var vehicleByCard = new Dictionary<string, DkvVehicleInfo>();
        foreach (var card in cardRows)
        {
            if (vehicles.TryGetValue(card.VehicleId, out var info))
            {
                vehicleByCard[card.CardNumber.ToLowerInvariant()] = info;
            }
        }

        var items = new List<DkvPlanItem>(settled);

        if (toMatch.Count > 0)
        {
            var available = await LoadAvailableExpensesAsync(
                context, toMatch.Select(m => m.Row).ToList(), vehicleByCard, unresolvedIds, cancellationToken);

            var matches = DkvMatcher.Match(
                toMatch.Select(m => m.Row).ToList(), vehicleByCard, available, knownLabels);

            for (var i = 0; i < toMatch.Count; i++)
            {
                var (row, existing, fromFile, outcome) = toMatch[i];

                items.Add(new DkvPlanItem
                {
                    Row = row,
                    Existing = existing,
                    FromFile = fromFile,
                    Outcome = outcome,
                    Match = matches[i],
                    RewritesMatch = true,
                });
            }
        }

        return new DkvPlan
        {
            Items = items.OrderBy(i => i.Row.RowNumber == 0 ? int.MaxValue : i.Row.RowNumber).ToList(),
            Vehicles = vehicles,
            VehiclesByTd = vehiclesByTd,
        };
    }

    private static async Task<List<DkvExpenseInfo>> LoadAvailableExpensesAsync(
        IApplicationDbContext context,
        IReadOnlyList<DkvRow> rows,
        IReadOnlyDictionary<string, DkvVehicleInfo> vehicleByCard,
        IReadOnlySet<Guid> reMatchedIds,
        CancellationToken cancellationToken)
    {
        var vehicleIds = rows
            .Select(r => vehicleByCard.TryGetValue(r.CardNumber.ToLowerInvariant(), out var v) ? v.VehicleId : (Guid?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (vehicleIds.Count == 0)
        {
            return [];
        }

        var from = rows.Min(r => r.Date).AddDays(-1);
        var to = rows.Max(r => r.Date).AddDays(1);

        // An entry already paired with a settled row is not up for grabs.
        var taken = await context.FuelTransactions
            .Where(t => t.VehicleExpenseId != null && !reMatchedIds.Contains(t.Id))
            .Select(t => t.VehicleExpenseId!.Value)
            .ToListAsync(cancellationToken);

        var takenSet = taken.ToHashSet();

        var expenses = await context.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.Kind == VehicleExpenseKind.Fuel
                && (vehicleIds.Contains(e.VehicleId) || e.FuelCardNumber != null)
                && e.OccurredOn >= from && e.OccurredOn <= to)
            .Select(e => new
            {
                e.Id,
                e.VehicleId,
                e.OccurredOn,
                e.Amount,
                e.FuelCardNumber,
                HasOdometer = e.OdometerKm != null,
                HasReceipt = context.Attachments.Any(a => a.VehicleExpenseId == e.Id)
            })
            .ToListAsync(cancellationToken);

        return expenses
            .Where(e => !takenSet.Contains(e.Id))
            .Select(e => new DkvExpenseInfo(e.Id, e.VehicleId, e.OccurredOn, e.Amount, e.HasOdometer, e.HasReceipt, e.FuelCardNumber))
            .ToList();
    }

    internal static string Key(DkvRow row) =>
        Key(row.CardNumber, row.Date, row.Time, row.ProductCode, row.Amount);

    internal static string Key(FuelTransaction t) =>
        Key(t.CardNumber, t.OccurredOn, t.OccurredAtTime, t.ProductCode, t.Amount);

    private static string Key(string card, DateOnly date, TimeOnly time, string code, decimal amount) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{card}|{date:yyyyMMdd}|{time:HHmm}|{code}|{amount:0.00}");

    private static DkvRow ToRow(FuelTransaction t) => new(
        0, t.CardNumber, t.StatementVehicleLabel, t.OccurredOn, t.OccurredAtTime, t.ProductGroup,
        t.ProductType, t.ProductCode, t.Amount, t.Currency, t.Country, t.IsInvoiced);
}
