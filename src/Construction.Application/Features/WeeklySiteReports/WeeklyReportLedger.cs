using System.Globalization;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports;

/// <summary>
/// Where an approved weekly report's hours go in the monthly sheet (the Ledger): the sheet of the month, the
/// section of the site, the row of the worker and the column of the calendar week, and what is already in that cell.
/// </summary>
public static class WeeklyReportLedger
{
    /// <summary>One month a report's hours land in.</summary>
    public readonly record struct YearMonth(int Year, int Number)
    {
        public string Label => $"{Number:D2}/{Year}";
    }

    /// <summary>The month of the week's Monday, and the month of its Sunday when that is a different one.</summary>
    public static (YearMonth First, YearMonth? Second) MonthsOf(int isoYear, int isoWeek)
    {
        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));
        var sunday = monday.AddDays(6);
        var first = new YearMonth(monday.Year, monday.Month);

        return sunday.Month == monday.Month && sunday.Year == monday.Year
            ? (first, null)
            : (first, new YearMonth(sunday.Year, sunday.Month));
    }

    /// <summary>
    /// The sheet that holds the site's hours for the month: one that has weekly hour columns, preferring the
    /// one that already has the site's section, then the one of the site's own business unit.
    /// </summary>
    public sealed record Target(Guid LedgerId, string LedgerName, bool Locked, Guid? WeekColumnId, Guid? SectionId);

    public static async Task<Target?> FindAsync(
        IApplicationDbContext context, Guid projectId, int isoYear, int isoWeek, YearMonth month, CancellationToken cancellationToken)
    {
        var branchId = await context.Projects
            .Where(p => p.Id == projectId)
            .Select(p => p.BranchId)
            .FirstOrDefaultAsync(cancellationToken);

        var candidates = await context.Ledgers
            .AsNoTracking()
            .Where(l => l.Year == month.Year && l.Month == month.Number
                && l.Columns.Any(c => c.SystemKey != null && c.SystemKey.StartsWith("week")))
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.BranchId,
                Locked = l.LockedAt != null,
                SectionId = l.Sections.Where(s => s.ProjectId == projectId).Select(s => (Guid?)s.Id).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var chosen = candidates
            .OrderByDescending(l => l.SectionId != null)
            .ThenByDescending(l => branchId != null && l.BranchId == branchId)
            .ThenBy(l => l.BranchId == null ? 0 : 1)
            .FirstOrDefault();

        if (chosen is null)
        {
            return null;
        }

        var weeks = LedgerTemplates.MonthWeeks(month.Year, month.Number);
        var index = weeks.ToList().FindIndex(w => w.IsoYear == isoYear && w.IsoWeek == isoWeek);

        Guid? columnId = null;

        if (index >= 0)
        {
            var key = LedgerTemplates.Keys.Week(index + 1);
            columnId = await context.LedgerColumns
                .Where(c => c.LedgerId == chosen.Id && c.SystemKey == key)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new Target(chosen.Id, chosen.Name, chosen.Locked, columnId, chosen.SectionId);
    }

    /// <summary>What the sheet holds now for a worker in the week's column of the month, or null for an empty cell.</summary>
    public static async Task<decimal?> ExistingAsync(
        IApplicationDbContext context, Target target, Guid projectId, Guid employeeId, CancellationToken cancellationToken)
    {
        if (target.SectionId is null || target.WeekColumnId is null)
        {
            return null;
        }

        var value = await context.LedgerCells
            .AsNoTracking()
            .Where(c => c.ColumnId == target.WeekColumnId
                && c.Row.SectionId == target.SectionId
                && c.Row.EmployeeId == employeeId)
            .Select(c => c.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(value) ? null : LedgerCellMath.ParseNumeric(value);
    }

    public static string Format(decimal hours) =>
        hours.ToString("0.##", CultureInfo.InvariantCulture);

    public sealed record WriteResult(int Written, int Unchanged, int Kept);

    /// <summary>
    /// Enters the report's hours into the sheets. An empty cell is filled; a cell that already says the same only
    /// gets marked as coming from the report; a cell that says something else is overwritten unless the worker is in
    /// <paramref name="keepExisting"/>. The caller saves, so a refusal on one month leaves nothing half-written.
    /// </summary>
    public static async Task<WriteResult> WriteAsync(
        IApplicationDbContext context,
        WeeklySiteReport report,
        IReadOnlySet<Guid> keepExisting,
        CancellationToken cancellationToken)
    {
        var (first, second) = MonthsOf(report.IsoYear, report.IsoWeek);
        var months = second is null ? new[] { first } : new[] { first, second.Value };

        // Every month is checked before anything is created or changed, so a missing or closed sheet for the
        // second month does not leave the first one half written.
        var plans = new List<(Target Target, YearMonth Month, List<(WeeklySiteReportLine Line, decimal Hours)> Lines)>();

        for (var i = 0; i < months.Length; i++)
        {
            var month = months[i];
            var forThisMonth = report.Lines
                .Select(l => (Line: l, Hours: i == 0 ? l.Hours - l.HoursSecondMonth : l.HoursSecondMonth))
                .Where(x => x.Hours > 0 || (i == 0 && second is null))
                .ToList();

            if (forThisMonth.Count == 0)
            {
                continue;
            }

            var target = await FindAsync(context, report.ProjectId, report.IsoYear, report.IsoWeek, month, cancellationToken)
                ?? throw new ConflictException(
                    $"There is no monthly sheet for {month.Label}. Create it first, then approve the report.");

            if (target.Locked)
            {
                throw new ConflictException(
                    $"The monthly sheet for {month.Label} is closed. Reopen it, with a reason, to enter these hours.");
            }

            if (target.WeekColumnId is null)
            {
                throw new ConflictException(
                    $"The monthly sheet for {month.Label} has no column for week {report.IsoWeek}.");
            }

            plans.Add((target, month, forThisMonth));
        }

        var written = 0;
        var unchanged = 0;
        var kept = 0;

        foreach (var (target, _, lines) in plans)
        {
            var sectionId = target.SectionId ?? await CreateSectionAsync(context, target.LedgerId, report.ProjectId, cancellationToken);

            foreach (var (line, hours) in lines)
            {
                var row = await context.LedgerRows
                    .FirstOrDefaultAsync(r => r.SectionId == sectionId && r.EmployeeId == line.EmployeeId, cancellationToken)
                    ?? await CreateRowAsync(context, sectionId, line.EmployeeId, cancellationToken);

                var cell = await context.LedgerCells
                    .FirstOrDefaultAsync(c => c.RowId == row.Id && c.ColumnId == target.WeekColumnId, cancellationToken);

                var existing = cell is null || string.IsNullOrWhiteSpace(cell.Value)
                    ? (decimal?)null
                    : LedgerCellMath.ParseNumeric(cell.Value);

                if (existing is not null && existing != hours && keepExisting.Contains(line.EmployeeId))
                {
                    kept++;
                    continue;
                }

                if (cell is null)
                {
                    cell = new LedgerCell { RowId = row.Id, ColumnId = target.WeekColumnId!.Value };
                    context.LedgerCells.Add(cell);
                }

                if (existing == hours)
                {
                    unchanged++;
                }
                else
                {
                    cell.Value = Format(hours);
                    written++;
                }

                cell.SourceReportId = report.Id;
            }
        }

        return new WriteResult(written, unchanged, kept);
    }

    /// <summary>Takes back what a report entered: the cells it filled are emptied, the ones somebody typed over are left.</summary>
    public static async Task RetractAsync(IApplicationDbContext context, Guid reportId, CancellationToken cancellationToken)
    {
        var cells = await context.LedgerCells
            .Where(c => c.SourceReportId == reportId)
            .ToListAsync(cancellationToken);

        foreach (var cell in cells)
        {
            cell.Value = null;
            cell.SourceReportId = null;
        }
    }

    private static async Task<Guid> CreateSectionAsync(
        IApplicationDbContext context, Guid ledgerId, Guid projectId, CancellationToken cancellationToken)
    {
        var name = await context.Projects
            .Where(p => p.Id == projectId)
            .Select(p => p.Name)
            .FirstAsync(cancellationToken);

        var order = await context.LedgerSections
            .Where(s => s.LedgerId == ledgerId)
            .Select(s => (int?)s.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var section = new LedgerSection { LedgerId = ledgerId, Name = name, ProjectId = projectId, SortOrder = order + 1 };
        context.LedgerSections.Add(section);
        await context.SaveChangesAsync(cancellationToken);

        return section.Id;
    }

    private static async Task<LedgerRow> CreateRowAsync(
        IApplicationDbContext context, Guid sectionId, Guid employeeId, CancellationToken cancellationToken)
    {
        var label = await context.Employees
            .Where(e => e.Id == employeeId)
            .Select(e => e.FirstName + " " + e.LastName)
            .FirstAsync(cancellationToken);

        var order = await context.LedgerRows
            .Where(r => r.SectionId == sectionId)
            .Select(r => (int?)r.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var row = new LedgerRow { SectionId = sectionId, Label = label, EmployeeId = employeeId, SortOrder = order + 1 };
        context.LedgerRows.Add(row);
        await context.SaveChangesAsync(cancellationToken);

        return row;
    }
}
