using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>
/// Every row of a ledger, in the order they appear on screen.
/// </summary>
/// <remarks>
/// The order matters: a cost that belongs to a person rather than a site goes on
/// their first row, and "first" is decided by this order. Loading them in one place
/// keeps the screen, the totals, the checks and the export agreeing on it.
/// </remarks>
public static class LedgerRowRefs
{
    public static async Task<IReadOnlyList<LedgerRowRef>> LoadAsync(
        IApplicationDbContext context,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        var rows = await context.LedgerRows
            .AsNoTracking()
            .Where(r => r.Section.LedgerId == ledgerId)
            .OrderBy(r => r.Section.SortOrder).ThenBy(r => r.Section.Id)
            .ThenBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => new { r.Id, r.EmployeeId, ProjectId = r.Section.ProjectId })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new LedgerRowRef(r.Id, r.EmployeeId, r.ProjectId)).ToList();
    }
}
