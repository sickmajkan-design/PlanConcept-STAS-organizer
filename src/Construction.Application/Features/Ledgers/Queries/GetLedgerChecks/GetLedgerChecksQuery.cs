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

    /// <summary>The other sections involved, for the hours check.</summary>
    public IReadOnlyList<string> OtherSections { get; init; } = [];
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
}

public class GetLedgerChecksQueryValidator : AbstractValidator<GetLedgerChecksQuery>
{
    public GetLedgerChecksQueryValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.ExpectedHours).InclusiveBetween(1m, 744m);
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

        var formulas = columns.ToDictionary(c => c.Id, c => LedgerFormula.Parse(c.FormulaJson));

        var calculator = new LedgerCalculator(columns.Select(c => (c.Id, formulas[c.Id])));

        var period = await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == request.LedgerId)
            .Select(l => new { l.Year, l.Month })
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

            if (hoursColumn is not null && clientRateColumn is not null && hours > 0 && ValueOf(clientRateColumn) == 0m)
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

            foreach (var row in linked)
            {
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

        return issues;
    }
}
