using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Queries.GetToolCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgerSectionRows;

/// <summary>
/// One section's rows and cells, fetched only once its table is actually
/// opened — the counterpart to the row-less shell <c>GetLedgerById</c> now
/// returns.
/// </summary>
public record GetLedgerSectionRowsQuery(Guid LedgerId, Guid SectionId) : IRequest<LedgerSectionDto>;

public class GetLedgerSectionRowsQueryHandler
    : IRequestHandler<GetLedgerSectionRowsQuery, LedgerSectionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public GetLedgerSectionRowsQueryHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<LedgerSectionDto> Handle(
        GetLedgerSectionRowsQuery request,
        CancellationToken cancellationToken)
    {
        var section = await _context.LedgerSections
            .AsNoTracking()
            .Where(s => s.Id == request.SectionId && s.LedgerId == request.LedgerId)
            .Select(LedgerSectionDetailMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (section is null)
        {
            throw new NotFoundException(nameof(LedgerSection), request.SectionId);
        }

        var sourcedColumns = await _context.LedgerColumns
            .AsNoTracking()
            .Where(c => c.LedgerId == request.LedgerId && c.SourceMetric != null)
            .Select(c => new { c.Id, Metric = c.SourceMetric!.Value })
            .ToListAsync(cancellationToken);

        if (sourcedColumns.Count == 0)
        {
            return section;
        }

        var ledgerPeriod = await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == request.LedgerId)
            .Select(l => new { l.Year, l.Month })
            .FirstAsync(cancellationToken);

        var from = new DateOnly(ledgerPeriod.Year, ledgerPeriod.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        return await LedgerSourcedValues.OverlayAsync(
            _context, _mediator, section, sourcedColumns.Select(c => (c.Id, c.Metric)), from, to,
            cancellationToken);
    }
}
