using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgerUnlinkedRows;

/// <summary>
/// Every row in this ledger with no Employee/Vehicle/Tool/Material link —
/// one lightweight query across the whole ledger, not "open every section
/// and look." Deliberately separate from the per-section row fetch: opening
/// every section at once to filter client-side is exactly the cost that
/// fetch's laziness exists to avoid, and doing it anyway on a real month
/// (dozens of sections, each with its own sourced-column computation) is
/// what overwhelmed the browser before this existed.
/// </summary>
public record GetLedgerUnlinkedRowsQuery(Guid LedgerId) : IRequest<IReadOnlyList<LedgerUnlinkedRowDto>>;

public class GetLedgerUnlinkedRowsQueryHandler
    : IRequestHandler<GetLedgerUnlinkedRowsQuery, IReadOnlyList<LedgerUnlinkedRowDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLedgerUnlinkedRowsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LedgerUnlinkedRowDto>> Handle(
        GetLedgerUnlinkedRowsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.LedgerRows
            .AsNoTracking()
            .Where(r => r.Section.LedgerId == request.LedgerId
                && r.EmployeeId == null
                && r.VehicleId == null
                && r.ToolId == null
                && r.MaterialId == null)
            .OrderBy(r => r.Section.SortOrder)
            .ThenBy(r => r.SortOrder)
            .Select(r => new LedgerUnlinkedRowDto
            {
                RowId = r.Id,
                RowLabel = r.Label,
                SectionId = r.SectionId,
                SectionName = r.Section.Name,
            })
            .ToListAsync(cancellationToken);
    }
}
