using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands.SetLedgerColor;

/// <summary>
/// Colors (or clears the color of) one whole row — a SuperAdmin's own visual
/// flag, e.g. "paid" or "needs attention", the same way the client uses fill
/// color in the spreadsheet this feature replaces. Kept separate from
/// <c>UpdateLedgerRowCommand</c> so picking a color never has to resend the
/// row's label or linked employee, and vice versa.
/// </summary>
public record SetLedgerRowColorCommand : IRequest
{
    public Guid RowId { get; init; }

    public string? Color { get; init; }
}

public class SetLedgerRowColorCommandValidator : AbstractValidator<SetLedgerRowColorCommand>
{
    public SetLedgerRowColorCommandValidator()
    {
        RuleFor(x => x.RowId).NotEmpty();
        RuleFor(x => x.Color).MaximumLength(20);
    }
}

public class SetLedgerRowColorCommandHandler : IRequestHandler<SetLedgerRowColorCommand>
{
    private readonly IApplicationDbContext _context;

    public SetLedgerRowColorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetLedgerRowColorCommand request, CancellationToken cancellationToken)
    {
        var row = await _context.LedgerRows
            .FirstOrDefaultAsync(r => r.Id == request.RowId, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerRow), request.RowId);

        row.ColorTag = request.Color;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Colors (or clears the color of) one cell. Upserts by (RowId, ColumnId)
/// like <c>SetLedgerCellCommand</c>, and — deliberately — never touches the
/// cell's <c>Value</c>: picking a color for a still-empty cell is a normal
/// thing to want, and setting a value should never require resending whatever
/// color happened to be picked.
/// </summary>
public record SetLedgerCellColorCommand : IRequest
{
    public Guid RowId { get; init; }

    public Guid ColumnId { get; init; }

    public string? Color { get; init; }
}

public class SetLedgerCellColorCommandValidator : AbstractValidator<SetLedgerCellColorCommand>
{
    public SetLedgerCellColorCommandValidator()
    {
        RuleFor(x => x.RowId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.Color).MaximumLength(20);
    }
}

public class SetLedgerCellColorCommandHandler : IRequestHandler<SetLedgerCellColorCommand>
{
    private readonly IApplicationDbContext _context;

    public SetLedgerCellColorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetLedgerCellColorCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.LedgerRows.AnyAsync(r => r.Id == request.RowId, cancellationToken))
        {
            throw new NotFoundException(nameof(LedgerRow), request.RowId);
        }

        if (!await _context.LedgerColumns.AnyAsync(c => c.Id == request.ColumnId, cancellationToken))
        {
            throw new NotFoundException(nameof(LedgerColumn), request.ColumnId);
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
                Value = null,
                ColorTag = request.Color,
            });
        }
        else
        {
            cell.ColorTag = request.Color;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
