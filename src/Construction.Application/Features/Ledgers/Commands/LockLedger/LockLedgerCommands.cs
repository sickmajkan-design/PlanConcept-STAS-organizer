using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands.LockLedger;

/// <summary>
/// Closes a month. From then on its cells, columns, sections and rows cannot be changed, and the month's
/// figures are kept as they were, so what went to the accountant stays what it was.
/// </summary>
public record LockLedgerCommand(Guid Id) : IRequest<LockedLedgerDto>;

/// <summary>Reopens a closed month. The reason is kept in the audit trail.</summary>
public record UnlockLedgerCommand : IRequest<LockedLedgerDto>
{
    /// <summary>Taken from the route.</summary>
    public Guid Id { get; init; }

    public string Reason { get; init; } = null!;
}

public class UnlockLedgerCommandValidator : AbstractValidator<UnlockLedgerCommand>
{
    public UnlockLedgerCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required to reopen a closed month.")
            .MaximumLength(500);
    }
}

public class LockedLedgerDto
{
    public Guid Id { get; init; }

    public DateTime? LockedAt { get; init; }

    public string? UnlockNote { get; init; }

    /// <summary>The figures kept when the month was closed. Null while it is open.</summary>
    public LedgerMonthResult? Result { get; init; }
}

public class LockLedgerHandlers :
    IRequestHandler<LockLedgerCommand, LockedLedgerDto>,
    IRequestHandler<UnlockLedgerCommand, LockedLedgerDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LockLedgerHandlers(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<LockedLedgerDto> Handle(LockLedgerCommand request, CancellationToken cancellationToken)
    {
        var ledger = await _context.Ledgers
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ledger), request.Id);

        if (ledger.LockedAt is not null)
        {
            throw new ConflictException("This month is already closed.");
        }

        var result = await LedgerMonthResults.ComputeAsync(_context, ledger.Id, cancellationToken);

        ledger.LockedAt = _dateTimeProvider.UtcNow;
        ledger.LockedByUserId = _currentUserService.UserId;
        ledger.ResultJson = result.ToJson();

        await _context.SaveChangesAsync(cancellationToken);

        return new LockedLedgerDto { Id = ledger.Id, LockedAt = ledger.LockedAt, Result = result };
    }

    public async Task<LockedLedgerDto> Handle(UnlockLedgerCommand request, CancellationToken cancellationToken)
    {
        var ledger = await _context.Ledgers
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ledger), request.Id);

        if (ledger.LockedAt is null)
        {
            throw new ConflictException("This month is not closed.");
        }

        ledger.LockedAt = null;
        ledger.LockedByUserId = null;
        ledger.ResultJson = null;
        ledger.UnlockNote = request.Reason.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new LockedLedgerDto { Id = ledger.Id, UnlockNote = ledger.UnlockNote };
    }
}
