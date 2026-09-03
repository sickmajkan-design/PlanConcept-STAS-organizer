using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands.DeleteLedger;

/// <summary>Soft-deletes a whole month's ledger, columns/sections/rows/cells and all.</summary>
public record DeleteLedgerCommand(Guid Id) : IRequest;

public class DeleteLedgerCommandHandler : IRequestHandler<DeleteLedgerCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteLedgerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLedgerCommand request, CancellationToken cancellationToken)
    {
        var ledger = await _context.Ledgers
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Ledger), request.Id);

        _context.Ledgers.Remove(ledger);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
