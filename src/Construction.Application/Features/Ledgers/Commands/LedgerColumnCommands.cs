using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands;

// Column commands, all together — each is a small, self-contained edit to
// one ledger's shape, the same grouping DeleteCostRecordCommands.cs uses for
// several small ledger deletes.

public record AddLedgerColumnCommand : IRequest<LedgerColumnDto>
{
    public Guid LedgerId { get; init; }

    public string Name { get; init; } = null!;

    public LedgerColumnDataType DataType { get; init; }
}

public class AddLedgerColumnCommandValidator : AbstractValidator<AddLedgerColumnCommand>
{
    public AddLedgerColumnCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataType).IsInEnum();
    }
}

public class AddLedgerColumnCommandHandler : IRequestHandler<AddLedgerColumnCommand, LedgerColumnDto>
{
    private readonly IApplicationDbContext _context;

    public AddLedgerColumnCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerColumnDto> Handle(
        AddLedgerColumnCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Ledgers.AnyAsync(l => l.Id == request.LedgerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Ledger), request.LedgerId);
        }

        var nextOrder = await _context.LedgerColumns
            .Where(c => c.LedgerId == request.LedgerId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var column = new LedgerColumn
        {
            LedgerId = request.LedgerId,
            Name = request.Name.Trim(),
            DataType = request.DataType,
            SortOrder = nextOrder + 1,
        };

        _context.LedgerColumns.Add(column);
        await _context.SaveChangesAsync(cancellationToken);

        return new LedgerColumnDto
        {
            Id = column.Id,
            Name = column.Name,
            DataType = column.DataType.ToString(),
            SortOrder = column.SortOrder,
        };
    }
}

public record UpdateLedgerColumnCommand : IRequest<LedgerColumnDto>
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public LedgerColumnDataType DataType { get; init; }
}

public class UpdateLedgerColumnCommandValidator : AbstractValidator<UpdateLedgerColumnCommand>
{
    public UpdateLedgerColumnCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataType).IsInEnum();
    }
}

public class UpdateLedgerColumnCommandHandler
    : IRequestHandler<UpdateLedgerColumnCommand, LedgerColumnDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateLedgerColumnCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerColumnDto> Handle(
        UpdateLedgerColumnCommand request,
        CancellationToken cancellationToken)
    {
        var column = await _context.LedgerColumns
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerColumn), request.Id);

        column.Name = request.Name.Trim();
        column.DataType = request.DataType;

        await _context.SaveChangesAsync(cancellationToken);

        return new LedgerColumnDto
        {
            Id = column.Id,
            Name = column.Name,
            DataType = column.DataType.ToString(),
            SortOrder = column.SortOrder,
        };
    }
}

/// <summary>Removes a column and every cell recorded under it.</summary>
public record DeleteLedgerColumnCommand(Guid Id) : IRequest;

public class DeleteLedgerColumnCommandHandler : IRequestHandler<DeleteLedgerColumnCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteLedgerColumnCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLedgerColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await _context.LedgerColumns
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerColumn), request.Id);

        _context.LedgerColumns.Remove(column);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Reorders every column of one ledger in one move — the full new order, not a delta.</summary>
public record ReorderLedgerColumnsCommand : IRequest
{
    public Guid LedgerId { get; init; }

    public IReadOnlyList<Guid> OrderedColumnIds { get; init; } = Array.Empty<Guid>();
}

public class ReorderLedgerColumnsCommandHandler : IRequestHandler<ReorderLedgerColumnsCommand>
{
    private readonly IApplicationDbContext _context;

    public ReorderLedgerColumnsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReorderLedgerColumnsCommand request, CancellationToken cancellationToken)
    {
        var columns = await _context.LedgerColumns
            .Where(c => c.LedgerId == request.LedgerId)
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        for (var i = 0; i < request.OrderedColumnIds.Count; i++)
        {
            if (columns.TryGetValue(request.OrderedColumnIds[i], out var column))
            {
                column.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
