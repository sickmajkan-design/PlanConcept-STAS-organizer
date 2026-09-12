using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands.UpdateLedger;

public record UpdateLedgerCommand : IRequest<LedgerDetailDto>
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public int Year { get; init; }

    public int Month { get; init; }

    public string? Note { get; init; }
}

public class UpdateLedgerCommandValidator : AbstractValidator<UpdateLedgerCommand>
{
    public UpdateLedgerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}

public class UpdateLedgerCommandHandler : IRequestHandler<UpdateLedgerCommand, LedgerDetailDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateLedgerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerDetailDto> Handle(
        UpdateLedgerCommand request,
        CancellationToken cancellationToken)
    {
        var ledger = await _context.Ledgers
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Ledger), request.Id);

        ledger.Name = request.Name.Trim();
        ledger.Year = request.Year;
        ledger.Month = request.Month;
        ledger.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == ledger.Id)
            .Select(LedgerShellMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
