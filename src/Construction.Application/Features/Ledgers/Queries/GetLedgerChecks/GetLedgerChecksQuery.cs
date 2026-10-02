using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgerChecks;

public static class LedgerCheckKinds
{
    /// <summary>A row with hours and no price to bill them at, so it costs money and earns none.</summary>
    public const string MissingClientRate = "MissingClientRate";

    /// <summary>A figure typed over a calculated column.</summary>
    public const string ManualOverride = "ManualOverride";

    /// <summary>The same person, across sections, has more hours than a month holds.</summary>
    public const string HoursAcrossSections = "HoursAcrossSections";

    /// <summary>Hours were worked and submitted but not yet approved, so they are not counted.</summary>
    public const string UnreviewedHours = "UnreviewedHours";

    /// <summary>A person with hours and nothing entered for contributions, which come off the payslip.</summary>
    public const string MissingContributions = "MissingContributions";

    /// <summary>
    /// The hours typed from the signed timesheets are far from what the app recorded
    /// as approved. Only a prompt to look: the signed hours are the record.
    /// </summary>
    public const string HoursDifferFromApp = "HoursDifferFromApp";

    /// <summary>
    /// A site billed by a fixed sum or by measured work has people working in the month and no invoice
    /// recorded for it, so nothing is billed.
    /// </summary>
    public const string MissingInvoice = "MissingInvoice";

    /// <summary>
    /// A site has hours typed for a calendar week and no scanned, client-signed timesheet filed
    /// against that week — the source those hours are supposed to come from. A prompt to look,
    /// not a block: the sheet may exist on paper and not be scanned in yet.
    /// </summary>
    public const string MissingSignedTimesheet = "MissingSignedTimesheet";

    /// <summary>
    /// A person in a business unit's payroll who that unit did not employ at any time in the month —
    /// they are on another unit's books, or on none. A prompt to look: the pay may belong here
    /// anyway, but then their employment in this unit is what is missing.
    /// </summary>
    public const string EmployeeInOtherUnit = "EmployeeInOtherUnit";
}

public class LedgerCheckDto
{
    public string Kind { get; init; } = null!;

    public Guid SectionId { get; init; }

    public string SectionName { get; init; } = null!;

    public Guid? RowId { get; init; }

    public string? RowLabel { get; init; }

    /// <summary>The column an override is in, where that is the point.</summary>
    public string? ColumnName { get; init; }

    /// <summary>Hours, for the hours check.</summary>
    public decimal? Amount { get; init; }

    /// <summary>What the app recorded, where the check compares the two.</summary>
    public decimal? ReferenceAmount { get; init; }

    /// <summary>The other sections involved, for the hours check.</summary>
    public IReadOnlyList<string> OtherSections { get; init; } = [];

    /// <summary>For the unit check: the unit that did employ the person that month, or null when none did.</summary>
    public string? OtherBranchName { get; init; }
}

/// <summary>
/// What is worth a second look before a month is closed and passed on.
/// </summary>
/// <remarks>
/// Only meaningful for a ledger made from the payroll template, whose columns
/// are found by what they are (<see cref="LedgerColumn.SystemKey"/>) rather than
/// by name, so renaming a column does not switch the checks off. A ledger with
/// none of those columns simply has nothing to check.
/// </remarks>
public record GetLedgerChecksQuery : IRequest<IReadOnlyList<LedgerCheckDto>>
{
    public Guid LedgerId { get; init; }

    /// <summary>Hours one person can plausibly work in the month.</summary>
    public decimal ExpectedHours { get; init; } = 176m;

    /// <summary>
    /// How far, in hours, the typed hours may be from the app's before it is worth
    /// mentioning. A few hours' difference is a forgotten break, not a mistake.
    /// </summary>
    public decimal AppHoursTolerance { get; init; } = 4m;
}

public class GetLedgerChecksQueryValidator : AbstractValidator<GetLedgerChecksQuery>
{
    public GetLedgerChecksQueryValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.ExpectedHours).InclusiveBetween(1m, 744m);
        RuleFor(x => x.AppHoursTolerance).InclusiveBetween(0m, 744m);
    }
}

public class GetLedgerChecksQueryHandler
    : IRequestHandler<GetLedgerChecksQuery, IReadOnlyList<LedgerCheckDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLedgerChecksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LedgerCheckDto>> Handle(
        GetLedgerChecksQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Ledgers.AnyAsync(l => l.Id == request.LedgerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Ledger), request.LedgerId);
        }

        var columns = await _context.LedgerColumns
            .AsNoTracking()
            .Where(c => c.LedgerId == request.LedgerId)
            .Select(c => new { c.Id, c.Name, c.SystemKey, c.FormulaJson })
            .ToListAsync(cancellationToken);

        var hoursColumn = columns.FirstOrDefault(c => c.SystemKey == LedgerTemplates.Keys.Hours)?.Id;
        var clientRateColumn = columns.FirstOrDefault(c => c.SystemKey == LedgerTemplates.Keys.ClientRate)?.Id;
        var contributionsColumn = columns.FirstOrDefault(c => c.SystemKey == LedgerTemplates.Keys.Contributions)?.Id;

        var formulas = columns.ToDictionary(c => c.Id, c => LedgerFormula.Parse(c.FormulaJson));

        var calculator = new LedgerCalculator(columns.Select(c => (c.Id, formulas[c.Id])));

        var period = await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == request.LedgerId)
            .Select(l => new { l.Year, l.Month, l.BranchId, BranchName = l.Branch != null ? l.Branch.Name : null })
            .FirstAsync(cancellationToken);

        var rows = await _context.LedgerRows
            .AsNoTracking()
            .Where(r => r.Section.LedgerId == request.LedgerId)
            .OrderBy(r => r.Section.SortOrder).ThenBy(r => r.SortOrder)
            .Select(r => new
            {
                r.Id,
                r.Label,
                r.EmployeeId,
                r.SectionId,
                SectionName = r.Section.Name,
                ProjectId = r.Section.ProjectId,
            })
            .ToListAsync(cancellationToken);

        var cells = (await _context.LedgerCells
                .AsNoTracking()
                .Where(c => c.Row.Section.LedgerId == request.LedgerId)
                .Select(c => new { c.RowId, c.ColumnId, c.Value })
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.RowId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.ColumnId, c => c.Value));

        var auto = await LedgerAutoValues.LoadAsync(
            _context,
            LedgerAutoValues.SourcesOf(columns.Select(c => (c.Id, c.FormulaJson))),
            await LedgerRowRefs.LoadAsync(_context, request.LedgerId, cancellationToken),
            cancellationToken);

        var issues = new List<LedgerCheckDto>();
        var hoursByRow = new Dictionary<Guid, decimal>();

        // A unit's payroll should hold the people that unit employed in the month.
        if (period.BranchId is { } ledgerBranchId)
        {
            var monthStart = new DateOnly(period.Year, period.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var people = rows.Where(r => r.EmployeeId is not null).Select(r => r.EmployeeId!.Value).Distinct().ToList();

            var employed = await _context.EmployeeBranches
                .AsNoTracking()
                .Where(p => people.Contains(p.EmployeeId)
                    && p.StartDate <= monthEnd && (p.EndDate == null || p.EndDate >= monthStart))
                .Select(p => new { p.EmployeeId, p.BranchId, BranchName = p.Branch.Name })
                .ToListAsync(cancellationToken);

            foreach (var row in rows.Where(r => r.EmployeeId is not null))
            {
                var theirs = employed.Where(e => e.EmployeeId == row.EmployeeId).ToList();

                if (theirs.Any(e => e.BranchId == ledgerBranchId))
                {
                    continue;
                }

                issues.Add(new LedgerCheckDto
                {
                    Kind = LedgerCheckKinds.EmployeeInOtherUnit,
                    SectionId = row.SectionId,
                    SectionName = row.SectionName,
                    RowId = row.Id,
                    RowLabel = row.Label,
                    OtherBranchName = theirs.Count == 0 ? null : string.Join(", ", theirs.Select(e => e.BranchName).Distinct()),
                });
            }
        }

        // How each site is billed. A price per hour is only expected where the site is billed by the
        // hour; a fixed-sum or measured site is billed by its invoices instead.
        var siteIds = rows.Where(r => r.ProjectId is not null).Select(r => r.ProjectId!.Value).Distinct().ToList();
        var billedByInvoice = (await _context.Projects
                .AsNoTracking()
                .Where(p => siteIds.Contains(p.Id) && p.BillingMode != ProjectBillingMode.Hourly)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var invoicedByProject = billedByInvoice.Count == 0
            ? new Dictionary<Guid, decimal>()
            : (await _context.Invoices
                    .AsNoTracking()
                    .Where(i => billedByInvoice.Contains(i.ProjectId)
                        && i.Status != InvoiceStatus.Cancelled
                        && i.PayrollYear == period.Year
                        && i.PayrollMonth == period.Month)
                    .GroupBy(i => i.ProjectId)
                    .Select(g => new { ProjectId = g.Key, Total = g.Sum(i => i.Amount) })
                    .ToListAsync(cancellationToken))
                .ToDictionary(x => x.ProjectId, x => x.Total);

        var noInvoiceReported = new HashSet<Guid>();

        // Whether the hour columns are filled from the app. Where they are, a figure
        // typed over one is already flagged as an override, and a second flag for the
        // same disagreement would be noise.
        var hoursFromApp = columns.Any(c =>
            c.SystemKey is not null
            && c.SystemKey.StartsWith("week", StringComparison.Ordinal)
            && formulas[c.Id]?.Source is not null);

        foreach (var row in rows)
        {
            var stored = cells.TryGetValue(row.Id, out var s) ? s : new Dictionary<Guid, string?>();
            var sourced = auto.TryGetValue(row.Id, out var a) ? a : null;
            var computed = calculator.ComputeRow(stored, sourced);

            decimal ValueOf(Guid? column) =>
                column is { } id
                    ? (computed.TryGetValue(id, out var c) ? c.Value : LedgerCellMath.ParseNumeric(stored.GetValueOrDefault(id)))
                    : 0m;

            var hours = ValueOf(hoursColumn);
            hoursByRow[row.Id] = hours;

            // Contributions come off the payslip and are entered every month. A worker
            // with hours and nothing there has not had theirs entered; a zero is an
            // answer, an empty cell is not.
            if (contributionsColumn is { } contributionsId
                && row.EmployeeId is not null
                && hours > 0
                && string.IsNullOrWhiteSpace(stored.GetValueOrDefault(contributionsId)))
            {
                issues.Add(new LedgerCheckDto
                {
                    Kind = LedgerCheckKinds.MissingContributions,
                    SectionId = row.SectionId,
                    SectionName = row.SectionName,
                    RowId = row.Id,
                    RowLabel = row.Label,
                    Amount = hours,
                });
            }

            var billedByInvoiceRow = row.ProjectId is { } rowProject && billedByInvoice.Contains(rowProject);

            if (billedByInvoiceRow
                && hours > 0
                && invoicedByProject.GetValueOrDefault(row.ProjectId!.Value) == 0m
                && noInvoiceReported.Add(row.SectionId))
            {
                issues.Add(new LedgerCheckDto
                {
                    Kind = LedgerCheckKinds.MissingInvoice,
                    SectionId = row.SectionId,
                    SectionName = row.SectionName,
                    Amount = hours,
                });
            }

            if (!billedByInvoiceRow && hoursColumn is not null && clientRateColumn is not null && hours > 0 && ValueOf(clientRateColumn) == 0m)
            {
                issues.Add(new LedgerCheckDto
                {
                    Kind = LedgerCheckKinds.MissingClientRate,
                    SectionId = row.SectionId,
                    SectionName = row.SectionName,
                    RowId = row.Id,
                    RowLabel = row.Label,
                    Amount = hours,
                });
            }

            // A figure typed over a calculation is worth a look. Over an automatic
            // figure it is only worth a look when it disagrees with it: hours typed
            // for someone whose timesheet is empty contradict nothing.
            foreach (var over in computed.Where(kv => kv.Value.IsOverride
                && (formulas[kv.Key]?.Source is null
                    || (formulas[kv.Key]!.HasOwnCalculation && !(sourced?.ContainsKey(kv.Key) ?? false))
                    || (sourced is not null
                        && sourced.TryGetValue(kv.Key, out var automatic)
                        && automatic > 0m
                        && automatic != kv.Value.Value))))
            {
                issues.Add(new LedgerCheckDto
                {
                    Kind = LedgerCheckKinds.ManualOverride,
                    SectionId = row.SectionId,
                    SectionName = row.SectionName,
                    RowId = row.Id,
                    RowLabel = row.Label,
                    ColumnName = columns.First(c => c.Id == over.Key).Name,
                });
            }
        }

        // Submitted but not approved: not counted above, and easy to forget.
        var linked = rows.Where(r => r.EmployeeId is not null && r.ProjectId is not null).ToList();

        if (linked.Count > 0)
        {
            var monthStart = new DateOnly(period.Year, period.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var employeeIds = linked.Select(r => r.EmployeeId!.Value).Distinct().ToList();
            var projectIds = linked.Select(r => r.ProjectId!.Value).Distinct().ToList();

            var waiting = await _context.TimeEntries
                .AsNoTracking()
                .Where(t => t.Status == TimeEntryStatus.Submitted
                    && t.EndedAt != null
                    && t.ProjectId != null
                    && employeeIds.Contains(t.EmployeeId)
                    && projectIds.Contains(t.ProjectId.Value))
                .Where(t => DateOnly.FromDateTime(t.StartedAt) >= monthStart
                    && DateOnly.FromDateTime(t.StartedAt) <= monthEnd)
                .Select(t => new
                {
                    t.EmployeeId,
                    ProjectId = t.ProjectId!.Value,
                    Minutes = (int)((t.EndedAt!.Value - t.StartedAt).TotalMinutes - t.BreakMinutes),
                })
                .ToListAsync(cancellationToken);

            // What the app recorded as approved, for comparison with the signed hours.
            var approved = hoursColumn is null || hoursFromApp
                ? []
                : await _context.TimeEntries
                    .AsNoTracking()
                    .Where(t => t.Status == TimeEntryStatus.Approved
                        && t.EndedAt != null
                        && t.ProjectId != null
                        && employeeIds.Contains(t.EmployeeId)
                        && projectIds.Contains(t.ProjectId.Value))
                    .Where(t => DateOnly.FromDateTime(t.StartedAt) >= monthStart
                        && DateOnly.FromDateTime(t.StartedAt) <= monthEnd)
                    .Select(t => new
                    {
                        t.EmployeeId,
                        ProjectId = t.ProjectId!.Value,
                        Minutes = (int)((t.EndedAt!.Value - t.StartedAt).TotalMinutes - t.BreakMinutes),
                    })
                    .ToListAsync(cancellationToken);

            foreach (var row in linked)
            {
                // Someone with nothing recorded in the app is not a disagreement: the
                // signed sheet may be the only record there is.
                var appMinutes = approved
                    .Where(a => a.EmployeeId == row.EmployeeId && a.ProjectId == row.ProjectId)
                    .Sum(a => a.Minutes);

                if (appMinutes > 0)
                {
                    var appHours = Math.Round(appMinutes / 60m, 2);
                    var typed = hoursByRow[row.Id];

                    if (Math.Abs(typed - appHours) > request.AppHoursTolerance)
                    {
                        issues.Add(new LedgerCheckDto
                        {
                            Kind = LedgerCheckKinds.HoursDifferFromApp,
                            SectionId = row.SectionId,
                            SectionName = row.SectionName,
                            RowId = row.Id,
                            RowLabel = row.Label,
                            Amount = typed,
                            ReferenceAmount = appHours,
                        });
                    }
                }

                var minutes = waiting
                    .Where(w => w.EmployeeId == row.EmployeeId && w.ProjectId == row.ProjectId)
                    .Sum(w => w.Minutes);

                if (minutes > 0)
                {
                    issues.Add(new LedgerCheckDto
                    {
                        Kind = LedgerCheckKinds.UnreviewedHours,
                        SectionId = row.SectionId,
                        SectionName = row.SectionName,
                        RowId = row.Id,
                        RowLabel = row.Label,
                        Amount = Math.Round(minutes / 60m, 2),
                    });
                }
            }
        }

        if (hoursColumn is not null)
        {
            foreach (var person in rows.Where(r => r.EmployeeId is not null).GroupBy(r => r.EmployeeId))
            {
                var total = person.Sum(r => hoursByRow[r.Id]);
                var sections = person.Select(r => r.SectionName).Distinct().ToList();

                if (sections.Count > 1 && total > request.ExpectedHours)
                {
                    var first = person.First();

                    issues.Add(new LedgerCheckDto
                    {
                        Kind = LedgerCheckKinds.HoursAcrossSections,
                        SectionId = first.SectionId,
                        SectionName = first.SectionName,
                        RowId = first.Id,
                        RowLabel = first.Label,
                        Amount = total,
                        OtherSections = sections.Where(n => n != first.SectionName).ToList(),
                    });
                }
            }
        }

        // A section's calendar week is flagged when it has hours typed and no scanned signed
        // timesheet filed for that project and week. One flag per section+week, not per person:
        // the sheet is one document for the whole site's week, not one per worker.
        var weekColumns = columns
            .Where(c => c.SystemKey is not null && c.SystemKey.StartsWith("week", StringComparison.Ordinal))
            .Select(c => (c.Id, Number: int.Parse(c.SystemKey!.AsSpan(4))))
            .ToList();

        if (weekColumns.Count > 0)
        {
            var monthWeeks = LedgerTemplates.MonthWeeks(period.Year, period.Month);
            var flaggedWeeks = new HashSet<(Guid SectionId, int Number)>();

            foreach (var row in rows.Where(r => r.ProjectId is not null))
            {
                var stored = cells.TryGetValue(row.Id, out var s) ? s : new Dictionary<Guid, string?>();
                var sourced = auto.TryGetValue(row.Id, out var a) ? a : null;
                var computed = calculator.ComputeRow(stored, sourced);

                foreach (var (columnId, number) in weekColumns)
                {
                    var value = computed.TryGetValue(columnId, out var c)
                        ? c.Value
                        : LedgerCellMath.ParseNumeric(stored.GetValueOrDefault(columnId));

                    if (value > 0 && number <= monthWeeks.Count)
                    {
                        flaggedWeeks.Add((row.SectionId, number));
                    }
                }
            }

            if (flaggedWeeks.Count > 0)
            {
                var projectBySection = rows
                    .Where(r => r.ProjectId is not null)
                    .GroupBy(r => r.SectionId)
                    .ToDictionary(g => g.Key, g => g.First().ProjectId!.Value);

                var neededKeys = flaggedWeeks
                    .Select(f => (
                        SectionId: f.SectionId,
                        Number: f.Number,
                        ProjectId: projectBySection[f.SectionId],
                        Year: monthWeeks[f.Number - 1].IsoYear,
                        Week: monthWeeks[f.Number - 1].IsoWeek))
                    .ToList();

                var neededProjectIds = neededKeys.Select(k => k.ProjectId).Distinct().ToList();

                var signedWithAttachment = (await _context.SignedTimesheets
                        .AsNoTracking()
                        .Where(s => neededProjectIds.Contains(s.ProjectId))
                        .Where(s => _context.Attachments.Any(a => a.SignedTimesheetId == s.Id))
                        .Select(s => new { s.ProjectId, s.Year, s.IsoWeek })
                        .ToListAsync(cancellationToken))
                    .Select(s => (s.ProjectId, s.Year, s.IsoWeek))
                    .ToHashSet();

                foreach (var key in neededKeys.OrderBy(k => k.Number))
                {
                    if (signedWithAttachment.Contains((key.ProjectId, key.Year, key.Week)))
                    {
                        continue;
                    }

                    var section = rows.First(r => r.SectionId == key.SectionId);

                    issues.Add(new LedgerCheckDto
                    {
                        Kind = LedgerCheckKinds.MissingSignedTimesheet,
                        SectionId = key.SectionId,
                        SectionName = section.SectionName,
                        ColumnName = columns.First(c => c.Id == weekColumns.First(w => w.Number == key.Number).Id).Name,
                    });
                }
            }
        }

        return issues;
    }
}
