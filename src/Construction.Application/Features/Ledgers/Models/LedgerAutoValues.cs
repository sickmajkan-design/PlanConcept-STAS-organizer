using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Costs;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>A ledger row, reduced to what an automatic figure is looked up by.</summary>
public sealed record LedgerRowRef(Guid RowId, Guid? EmployeeId, Guid? ProjectId);

/// <summary>
/// The figures the system already knows for a month's rows — hours worked, the
/// hourly rate, fuel, rented cars and housing — so the owner does not type them
/// a second time.
/// </summary>
/// <remarks>
/// <para>
/// <b>Rows must be passed for the whole ledger, in the order they appear</b> (section
/// order, then row order). Some costs belong to a person, not to a site, and such a
/// cost goes on that person's <em>first</em> row — otherwise someone listed under two
/// clients would have the same fuel bill counted twice. Knowing where the first row
/// is means seeing every row.
/// </para>
/// <para>
/// Hours are those of <em>approved</em> time entries, the same rule payroll exports
/// follow, counted for the section's own project so a person split across two sites
/// shows each site's share. Fuel is approved fuel expenses of the vehicles assigned
/// to the person. A rented car is the monthly rental of those vehicles, prorated by
/// the days it applied. Housing is the person's share of an accommodation's rent as
/// the accommodation pages compute it; a stay tied to a project lands on that
/// project's row, and any that is not (or has no matching row) on the first row.
/// </para>
/// <para>
/// A row with no employee has no automatic figure at all (a subcontractor, a crew):
/// it is absent from the result, which is how the calculator knows a value typed
/// there is ordinary input.
/// </para>
/// </remarks>
public static class LedgerAutoValues
{
    /// <summary>row id → column id → figure, only for rows the system can say something about.</summary>
    public static async Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<Guid, decimal>>> LoadAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<(Guid ColumnId, LedgerFormulaSource Source)> columns,
        IReadOnlyList<LedgerRowRef> rows,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Dictionary<Guid, decimal>>();

        Dictionary<Guid, decimal> For(Guid rowId) =>
            result.TryGetValue(rowId, out var d) ? d : result[rowId] = new Dictionary<Guid, decimal>();

        IReadOnlyDictionary<Guid, IReadOnlyDictionary<Guid, decimal>> Done() =>
            result.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<Guid, decimal>)kv.Value);

        if (columns.Count == 0 || rows.Count == 0)
        {
            return Done();
        }

        // Each person's first row, and the row a project's share of their costs belongs on.
        var firstRow = new Dictionary<Guid, LedgerRowRef>();
        var rowByProject = new Dictionary<(Guid Employee, Guid Project), LedgerRowRef>();

        foreach (var row in rows.Where(r => r.EmployeeId is not null))
        {
            firstRow.TryAdd(row.EmployeeId!.Value, row);

            if (row.ProjectId is { } project)
            {
                rowByProject.TryAdd((row.EmployeeId.Value, project), row);
            }
        }

        // ---- hours -------------------------------------------------------

        var hourColumns = columns.Where(c => c.Source.Kind == LedgerSourceKinds.TimeEntryHours).ToList();
        var hourRows = rows.Where(r => r.EmployeeId is not null && r.ProjectId is not null).ToList();

        if (hourColumns.Count > 0 && hourRows.Count > 0)
        {
            var employeeIds = hourRows.Select(r => r.EmployeeId!.Value).Distinct().ToList();
            var projectIds = hourRows.Select(r => r.ProjectId!.Value).Distinct().ToList();
            var from = hourColumns.Min(c => c.Source.From);
            var to = hourColumns.Max(c => c.Source.To);

            var entries = await context.TimeEntries
                .AsNoTracking()
                .Where(t => t.Status == TimeEntryStatus.Approved
                    && t.EndedAt != null
                    && t.ProjectId != null
                    && employeeIds.Contains(t.EmployeeId)
                    && projectIds.Contains(t.ProjectId.Value))
                .Where(t => DateOnly.FromDateTime(t.StartedAt) >= from
                    && DateOnly.FromDateTime(t.StartedAt) <= to)
                .Select(t => new
                {
                    t.EmployeeId,
                    ProjectId = t.ProjectId!.Value,
                    Day = DateOnly.FromDateTime(t.StartedAt),
                    Minutes = (int)((t.EndedAt!.Value - t.StartedAt).TotalMinutes - t.BreakMinutes),
                })
                .ToListAsync(cancellationToken);

            foreach (var row in hourRows)
            {
                var own = entries.Where(e => e.EmployeeId == row.EmployeeId && e.ProjectId == row.ProjectId).ToList();

                foreach (var (columnId, source) in hourColumns)
                {
                    var minutes = own.Where(e => e.Day >= source.From && e.Day <= source.To).Sum(e => e.Minutes);

                    For(row.RowId)[columnId] = Math.Round(minutes / 60m, 2);
                }
            }
        }

        // ---- hourly rate -------------------------------------------------

        var rateColumns = columns.Where(c => c.Source.Kind == LedgerSourceKinds.EmployeeHourlyRate).ToList();
        var employeeRows = rows.Where(r => r.EmployeeId is not null).ToList();

        if (rateColumns.Count > 0 && employeeRows.Count > 0)
        {
            var employeeIds = employeeRows.Select(r => r.EmployeeId!.Value).Distinct().ToList();

            var rates = await context.EmployeeRates
                .AsNoTracking()
                .Where(r => employeeIds.Contains(r.EmployeeId)
                    && r.RateType == RateType.Hourly
                    && r.HourlyRate != null)
                .Select(r => new { r.EmployeeId, r.StartDate, r.EndDate, Rate = r.HourlyRate!.Value })
                .ToListAsync(cancellationToken);

            foreach (var row in employeeRows)
            {
                foreach (var (columnId, source) in rateColumns)
                {
                    // The rate in force on the last day of the period; a person who
                    // left mid-month, or whose rate starts later, has none to offer.
                    var rate = rates
                        .Where(r => r.EmployeeId == row.EmployeeId
                            && r.StartDate <= source.To
                            && (r.EndDate == null || r.EndDate >= source.To))
                        .OrderByDescending(r => r.StartDate)
                        .Select(r => (decimal?)r.Rate)
                        .FirstOrDefault();

                    For(row.RowId)[columnId] = rate ?? 0m;
                }
            }
        }

        // ---- fuel and rented cars: the vehicles assigned to the person ------

        var fuelColumns = columns.Where(c => c.Source.Kind == LedgerSourceKinds.VehicleFuelCost).ToList();
        var rentalColumns = columns.Where(c => c.Source.Kind == LedgerSourceKinds.VehicleRentalCost).ToList();

        if ((fuelColumns.Count > 0 || rentalColumns.Count > 0) && firstRow.Count > 0)
        {
            var employeeIds = firstRow.Keys.ToList();

            var vehicles = await context.Vehicles
                .AsNoTracking()
                .Where(v => v.AssignedEmployeeId != null && employeeIds.Contains(v.AssignedEmployeeId.Value))
                .Select(v => new { v.Id, EmployeeId = v.AssignedEmployeeId!.Value })
                .ToListAsync(cancellationToken);

            var vehicleIds = vehicles.Select(v => v.Id).ToList();

            if (vehicleIds.Count > 0 && fuelColumns.Count > 0)
            {
                var from = fuelColumns.Min(c => c.Source.From);
                var to = fuelColumns.Max(c => c.Source.To);

                var fuel = await context.VehicleExpenses
                    .AsNoTracking()
                    .Where(e => vehicleIds.Contains(e.VehicleId)
                        && e.Kind == VehicleExpenseKind.Fuel
                        && e.Status == VehicleExpenseStatus.Approved
                        && e.OccurredOn >= from
                        && e.OccurredOn <= to)
                    .Select(e => new { e.VehicleId, e.OccurredOn, e.Amount })
                    .ToListAsync(cancellationToken);

                foreach (var (employeeId, row) in firstRow)
                {
                    var mine = vehicles.Where(v => v.EmployeeId == employeeId).Select(v => v.Id).ToHashSet();

                    foreach (var (columnId, source) in fuelColumns)
                    {
                        For(row.RowId)[columnId] = fuel
                            .Where(f => mine.Contains(f.VehicleId) && f.OccurredOn >= source.From && f.OccurredOn <= source.To)
                            .Sum(f => f.Amount);
                    }
                }
            }

            if (vehicleIds.Count > 0 && rentalColumns.Count > 0)
            {
                var from = rentalColumns.Min(c => c.Source.From);
                var to = rentalColumns.Max(c => c.Source.To);

                var rentals = await context.VehicleRentalRates
                    .AsNoTracking()
                    .Where(r => vehicleIds.Contains(r.VehicleId)
                        && r.StartDate <= to
                        && (r.EndDate == null || r.EndDate >= from))
                    .Select(r => new { r.VehicleId, r.StartDate, r.EndDate, r.MonthlyAmount })
                    .ToListAsync(cancellationToken);

                foreach (var (employeeId, row) in firstRow)
                {
                    var mine = vehicles.Where(v => v.EmployeeId == employeeId).Select(v => v.Id).ToHashSet();

                    foreach (var (columnId, source) in rentalColumns)
                    {
                        // The monthly amount for the days it applied, out of the days in
                        // the period's month: a whole month costs exactly the amount.
                        var days = DateTime.DaysInMonth(source.To.Year, source.To.Month);

                        For(row.RowId)[columnId] = Math.Round(
                            rentals
                                .Where(r => mine.Contains(r.VehicleId))
                                .Sum(r =>
                                {
                                    var start = r.StartDate > source.From ? r.StartDate : source.From;
                                    var end = r.EndDate is { } e && e < source.To ? e : source.To;
                                    var overlap = end.DayNumber - start.DayNumber + 1;

                                    return overlap > 0 ? r.MonthlyAmount * overlap / days : 0m;
                                }),
                            2);
                    }
                }
            }
            else if (rentalColumns.Count > 0)
            {
                foreach (var (_, row) in firstRow)
                {
                    foreach (var (columnId, _) in rentalColumns)
                    {
                        For(row.RowId)[columnId] = 0m;
                    }
                }
            }

            // A person with no vehicle has zero fuel: still an automatic figure.
            if (vehicleIds.Count == 0)
            {
                foreach (var (_, row) in firstRow)
                {
                    foreach (var (columnId, _) in fuelColumns)
                    {
                        For(row.RowId)[columnId] = 0m;
                    }
                }
            }
        }

        // ---- housing -----------------------------------------------------

        var housingColumns = columns.Where(c => c.Source.Kind == LedgerSourceKinds.AccommodationCost).ToList();

        if (housingColumns.Count > 0 && firstRow.Count > 0)
        {
            var from = housingColumns.Min(c => c.Source.From);
            var to = housingColumns.Max(c => c.Source.To);
            var employeeIds = firstRow.Keys.ToList();

            var accommodationIds = await context.AccommodationStays
                .AsNoTracking()
                .Where(s => employeeIds.Contains(s.EmployeeId)
                    && s.StartDate <= to
                    && (s.EndDate == null || s.EndDate >= from))
                .Select(s => s.AccommodationId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Rent is split among everyone in the place, so all of its occupants are loaded.
            var stays = await context.AccommodationStays
                .AsNoTracking()
                .Where(s => accommodationIds.Contains(s.AccommodationId)
                    && s.StartDate <= to
                    && (s.EndDate == null || s.EndDate >= from))
                .ToListAsync(cancellationToken);

            var accommodationRates = await context.AccommodationRates
                .AsNoTracking()
                .Where(r => accommodationIds.Contains(r.AccommodationId)
                    && r.StartDate <= to
                    && (r.EndDate == null || r.EndDate >= from))
                .ToListAsync(cancellationToken);

            var none = new Dictionary<Guid, string>();

            foreach (var (columnId, source) in housingColumns)
            {
                var shares = new List<ProjectEmployeeShare>();

                foreach (var accommodationId in accommodationIds)
                {
                    shares.AddRange(AccommodationCostCalculator.Calculate(
                        accommodationRates.Where(r => r.AccommodationId == accommodationId).ToList(),
                        stays.Where(s => s.AccommodationId == accommodationId).ToList(),
                        source.From,
                        source.To,
                        none,
                        none).ByProjectEmployee);
                }

                foreach (var (employeeId, first) in firstRow)
                {
                    For(first.RowId).TryAdd(columnId, 0m);

                    foreach (var share in shares.Where(s => s.EmployeeId == employeeId))
                    {
                        // Onto the row of the project the stay is for; failing that, the first row.
                        var target = share.ProjectId is { } p && rowByProject.TryGetValue((employeeId, p), out var match)
                            ? match
                            : first;

                        var figures = For(target.RowId);
                        figures[columnId] = figures.GetValueOrDefault(columnId) + share.Cost;
                    }
                }

                // Every other row of a person is still one the system knows about.
                foreach (var row in employeeRows)
                {
                    For(row.RowId).TryAdd(columnId, 0m);
                }
            }
        }

        // ---- refunds -----------------------------------------------------

        var refundColumns = columns.Where(c => c.Source.Kind == LedgerSourceKinds.EmployeeRefunds).ToList();

        if (refundColumns.Count > 0 && firstRow.Count > 0)
        {
            var employeeIds = firstRow.Keys.ToList();

            var refunds = await context.Refunds
                .AsNoTracking()
                .Where(r => r.Status == RefundStatus.Approved
                    && employeeIds.Contains(r.EmployeeId)
                    && r.PayrollYear != null
                    && r.PayrollMonth != null)
                .Select(r => new { r.EmployeeId, Year = r.PayrollYear!.Value, Month = r.PayrollMonth!.Value, r.Amount })
                .ToListAsync(cancellationToken);

            foreach (var (columnId, source) in refundColumns)
            {
                foreach (var (employeeId, first) in firstRow)
                {
                    // A person's own money goes on their first row only, like fuel: someone
                    // on two clients must not be paid back twice.
                    For(first.RowId)[columnId] = refunds
                        .Where(r => r.EmployeeId == employeeId && r.Year == source.From.Year && r.Month == source.From.Month)
                        .Sum(r => r.Amount);
                }

                foreach (var row in employeeRows)
                {
                    For(row.RowId).TryAdd(columnId, 0m);
                }
            }
        }

        return Done();
    }

    /// <summary>The sourced columns among a ledger's columns, with their parsed source.</summary>
    public static IReadOnlyList<(Guid ColumnId, LedgerFormulaSource Source)> SourcesOf(
        IEnumerable<(Guid Id, string? FormulaJson)> columns) =>
        columns
            .Select(c => (c.Id, Formula: LedgerFormula.Parse(c.FormulaJson)))
            .Where(c => c.Formula?.Source is not null)
            .Select(c => (c.Id, c.Formula!.Source!))
            .ToList();
}
