using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands.SetLedgerCell;

/// <summary>
/// Sets one cell's value — called on every edit the SuperAdmin makes to the
/// grid. Upserts by (RowId, ColumnId): the first edit to a cell inserts it,
/// every edit after that just updates the one row already there.
/// </summary>
public record SetLedgerCellCommand : IRequest
{
    public Guid RowId { get; init; }

    public Guid ColumnId { get; init; }

    public string? Value { get; init; }
}

public class SetLedgerCellCommandValidator : AbstractValidator<SetLedgerCellCommand>
{
    public SetLedgerCellCommandValidator()
    {
        RuleFor(x => x.RowId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.Value).MaximumLength(2000);
    }
}

public class SetLedgerCellCommandHandler : IRequestHandler<SetLedgerCellCommand>
{
    private readonly IApplicationDbContext _context;

    public SetLedgerCellCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetLedgerCellCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.LedgerRows.AnyAsync(r => r.Id == request.RowId, cancellationToken))
        {
            throw new NotFoundException(nameof(LedgerRow), request.RowId);
        }

        if (!await _context.LedgerColumns.AnyAsync(c => c.Id == request.ColumnId, cancellationToken))
        {
            throw new NotFoundException(nameof(LedgerColumn), request.ColumnId);
        }

        var value = request.Value?.Trim();
        if (value == string.Empty)
        {
            value = null;
        }

        var cell = await _context.LedgerCells
            .FirstOrDefaultAsync(
                c => c.RowId == request.RowId && c.ColumnId == request.ColumnId,
                cancellationToken);

        if (cell is null)
        {
            _context.LedgerCells.Add(new LedgerCell
            {
                RowId = request.RowId,
                ColumnId = request.ColumnId,
                Value = value,
            });
        }
        else
        {
            cell.Value = value;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
