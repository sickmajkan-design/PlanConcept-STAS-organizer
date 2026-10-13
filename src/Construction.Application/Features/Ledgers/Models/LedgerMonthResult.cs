using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>
/// A month's figures, worked out from the payroll template's columns: what was billed, what each pay group
/// earned, the costs that belong to the sites, and the month's result.
/// </summary>
/// <remarks>
/// Earnings are gross, that is before the advances typed in the sheet, because an advance is only the moment
/// of payment and does not change what the firm earned. <see cref="Result"/> is the sheet's own result column
/// added up, before the firm's fixed costs.
/// </remarks>
public sealed record LedgerMonthResult(
    decimal Billing,
    decimal WorkersEarned,
    decimal SubcontractorsEarned,
    decimal OfficeEarned,
    decimal Contributions,
    decimal Rent,
    decimal Fuel,
    decimal Housing,
    decimal Bonus,
    decimal Hours,
    decimal Result,
    IReadOnlyList<SiteFigures>? Sites = null)
{
    public static readonly LedgerMonthResult Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public decimal EarnedTotal => WorkersEarned + SubcontractorsEarned + OfficeEarned;

    /// <summary>Costs that belong to the sites: contributions, rented cars, fuel, housing and the year-end bonus.</summary>
    public decimal SiteCosts => Contributions + Rent + Fuel + Housing + Bonus;

    public static LedgerMonthResult operator +(LedgerMonthResult a, LedgerMonthResult b) => new(
        a.Billing + b.Billing,
        a.WorkersEarned + b.WorkersEarned,
        a.SubcontractorsEarned + b.SubcontractorsEarned,
        a.OfficeEarned + b.OfficeEarned,
        a.Contributions + b.Contributions,
        a.Rent + b.Rent,
        a.Fuel + b.Fuel,
        a.Housing + b.Housing,
        a.Bonus + b.Bonus,
        a.Hours + b.Hours,
        a.Result + b.Result);

    public string ToJson() => JsonSerializer.Serialize(this);

    public static LedgerMonthResult? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LedgerMonthResult>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>One site's (one section's) figures for the month, kept with it when it is closed.</summary>
public sealed record SiteFigures(
    Guid SectionId,
    string Name,
    Guid? ProjectId,
    decimal Hours,
    decimal Billing,
    decimal Wage,
    decimal Costs);

public static class LedgerMonthResults
{
    /// <summary>Works a ledger's figures out live from its cells and the system's sources.</summary>
    public static async Task<LedgerMonthResult> ComputeAsync(
        IApplicationDbContext context, Guid ledgerId, CancellationToken cancellationToken)
    {
        var keys = new[]
        {
            LedgerTemplates.Keys.Billing, LedgerTemplates.Keys.Pay, LedgerTemplates.Keys.Advance,
            LedgerTemplates.Keys.Hours, LedgerTemplates.Keys.Contributions, LedgerTemplates.Keys.Rent,
            LedgerTemplates.Keys.Fuel, LedgerTemplates.Keys.Housing, LedgerTemplates.Keys.Bonus,
            LedgerTemplates.Keys.Result,
        };

        var columns = await context.LedgerColumns
            .AsNoTracking()
            .Where(c => c.LedgerId == ledgerId && c.SystemKey != null && keys.Contains(c.SystemKey))
            .Select(c => new { c.Id, c.SystemKey })
            .ToListAsync(cancellationToken);

        if (columns.Count == 0)
        {
            return LedgerMonthResult.Empty;
        }

        var byKey = columns.ToDictionary(c => c.SystemKey!, c => c.Id);

        var figures = await LedgerColumnTotals.PerRowManyAsync(
            context, ledgerId, columns.Select(c => c.Id).ToList(), cancellationToken);

        var rowInfo = await context.LedgerRows
            .AsNoTracking()
            .Where(r => r.Section.LedgerId == ledgerId)
            .Select(r => new { r.Id, r.PayGroup, r.SectionId })
            .ToListAsync(cancellationToken);

        var groups = rowInfo.ToDictionary(r => r.Id, r => r.PayGroup);
        var sectionOf = rowInfo.ToDictionary(r => r.Id, r => r.SectionId);

        var sections = await context.LedgerSections
            .AsNoTracking()
            .Where(s => s.LedgerId == ledgerId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new { s.Id, s.Name, s.ProjectId })
            .ToListAsync(cancellationToken);

        var siteTotals = sections.ToDictionary(
            s => s.Id, _ => new decimal[4]); // hours, billing, wage, costs

        decimal Figure(Guid row, string key) =>
            byKey.TryGetValue(key, out var column)
            && figures.TryGetValue(row, out var perRow)
            && perRow.TryGetValue(column, out var value)
                ? value
                : 0m;

        decimal workers = 0, subcontractors = 0, office = 0;
        decimal billing = 0, contributions = 0, rent = 0, fuel = 0, housing = 0, bonus = 0, hours = 0, result = 0;

        foreach (var (rowId, group) in groups)
        {
            var earned = Figure(rowId, LedgerTemplates.Keys.Pay) + Figure(rowId, LedgerTemplates.Keys.Advance);

            switch (group)
            {
                case PayoutGroup.Subcontractors:
                    subcontractors += earned;
                    break;
                case PayoutGroup.Office:
                    office += earned;
                    break;
                default:
                    workers += earned;
                    break;
            }

            billing += Figure(rowId, LedgerTemplates.Keys.Billing);
            contributions += Figure(rowId, LedgerTemplates.Keys.Contributions);
            rent += Figure(rowId, LedgerTemplates.Keys.Rent);
            fuel += Figure(rowId, LedgerTemplates.Keys.Fuel);
            housing += Figure(rowId, LedgerTemplates.Keys.Housing);
            bonus += Figure(rowId, LedgerTemplates.Keys.Bonus);
            hours += Figure(rowId, LedgerTemplates.Keys.Hours);
            result += Figure(rowId, LedgerTemplates.Keys.Result);

            if (siteTotals.TryGetValue(sectionOf[rowId], out var site))
            {
                site[0] += Figure(rowId, LedgerTemplates.Keys.Hours);
                site[1] += Figure(rowId, LedgerTemplates.Keys.Billing);
                site[2] += earned;
                site[3] += Figure(rowId, LedgerTemplates.Keys.Contributions)
                    + Figure(rowId, LedgerTemplates.Keys.Rent)
                    + Figure(rowId, LedgerTemplates.Keys.Fuel)
                    + Figure(rowId, LedgerTemplates.Keys.Housing)
                    + Figure(rowId, LedgerTemplates.Keys.Bonus);
            }
        }

        var sites = sections
            .Select(s => new SiteFigures(
                s.Id, s.Name, s.ProjectId,
                siteTotals[s.Id][0], siteTotals[s.Id][1], siteTotals[s.Id][2], siteTotals[s.Id][3]))
            .ToList();

        return new LedgerMonthResult(
            billing, workers, subcontractors, office, contributions, rent, fuel, housing, bonus, hours, result, sites);
    }
}
