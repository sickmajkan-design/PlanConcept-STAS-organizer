using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgerById;

public record GetLedgerByIdQuery(Guid Id) : IRequest<LedgerDetailDto>;

public class GetLedgerByIdQueryHandler : IRequestHandler<GetLedgerByIdQuery, LedgerDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetLedgerByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerDetailDto> Handle(
        GetLedgerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ledger = await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == request.Id)
            .Select(LedgerShellMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return ledger ?? throw new NotFoundException(nameof(Ledger), request.Id);
    }
}
