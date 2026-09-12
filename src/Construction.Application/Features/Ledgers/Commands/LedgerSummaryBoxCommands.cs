using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands;

// Summary-box commands, all together — the same grouping the column/section/row
// command files already use for one ledger's small, self-contained edits.

public record AddLedgerSummaryBoxCommand : IRequest<LedgerSummaryBoxDto>
{
    public Guid LedgerId { get; init; }

    public string Label { get; init; } = null!;

    public Guid? SourceColumnId { get; init; }

    public decimal? ManualValue { get; init; }

    public int Sign { get; init; } = 1;

    public string? Color { get; init; }
}

public class AddLedgerSummaryBoxCommandValidator : AbstractValidator<AddLedgerSummaryBoxCommand>
{
    public AddLedgerSummaryBoxCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sign).Must(s => s == 1 || s == -1)
            .WithMessage("Sign must be 1 or -1.");
        RuleFor(x => x.Color).MaximumLength(20);
    }
}

public class AddLedgerSummaryBoxCommandHandler
    : IRequestHandler<AddLedgerSummaryBoxCommand, LedgerSummaryBoxDto>
{
    private readonly IApplicationDbContext _context;

    public AddLedgerSummaryBoxCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerSummaryBoxDto> Handle(
        AddLedgerSummaryBoxCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Ledgers.AnyAsync(l => l.Id == request.LedgerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Ledger), request.LedgerId);
        }

        var nextOrder = await _context.LedgerSummaryBoxes
            .Where(b => b.LedgerId == request.LedgerId)
            .Select(b => (int?)b.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var box = new LedgerSummaryBox
        {
            LedgerId = request.LedgerId,
            Label = request.Label.Trim(),
            SourceColumnId = request.SourceColumnId,
            ManualValue = request.SourceColumnId is null ? request.ManualValue : null,
            Sign = request.Sign,
            Color = request.Color,
            SortOrder = nextOrder + 1,
        };

        _context.LedgerSummaryBoxes.Add(box);
        await _context.SaveChangesAsync(cancellationToken);

        return await LedgerSummaryBoxMapping.ToDtoAsync(_context, box, cancellationToken);
    }
}

public record UpdateLedgerSummaryBoxCommand : IRequest<LedgerSummaryBoxDto>
{
    public Guid Id { get; init; }

    public string Label { get; init; } = null!;

    public Guid? SourceColumnId { get; init; }

    public decimal? ManualValue { get; init; }

    public int Sign { get; init; } = 1;

    public string? Color { get; init; }
}

public class UpdateLedgerSummaryBoxCommandValidator : AbstractValidator<UpdateLedgerSummaryBoxCommand>
{
    public UpdateLedgerSummaryBoxCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sign).Must(s => s == 1 || s == -1)
            .WithMessage("Sign must be 1 or -1.");
        RuleFor(x => x.Color).MaximumLength(20);
    }
}

public class UpdateLedgerSummaryBoxCommandHandler
    : IRequestHandler<UpdateLedgerSummaryBoxCommand, LedgerSummaryBoxDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateLedgerSummaryBoxCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerSummaryBoxDto> Handle(
        UpdateLedgerSummaryBoxCommand request,
        CancellationToken cancellationToken)
    {
        var box = await _context.LedgerSummaryBoxes
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerSummaryBox), request.Id);

        box.Label = request.Label.Trim();
        box.SourceColumnId = request.SourceColumnId;
        box.ManualValue = request.SourceColumnId is null ? request.ManualValue : null;
        box.Sign = request.Sign;
        box.Color = request.Color;

        await _context.SaveChangesAsync(cancellationToken);

        return await LedgerSummaryBoxMapping.ToDtoAsync(_context, box, cancellationToken);
    }
}

public record DeleteLedgerSummaryBoxCommand(Guid Id) : IRequest;

public class DeleteLedgerSummaryBoxCommandHandler : IRequestHandler<DeleteLedgerSummaryBoxCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteLedgerSummaryBoxCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLedgerSummaryBoxCommand request, CancellationToken cancellationToken)
    {
        var box = await _context.LedgerSummaryBoxes
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerSummaryBox), request.Id);

        _context.LedgerSummaryBoxes.Remove(box);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Reorders every summary box of one ledger in one move — the full new order, not a delta.</summary>
public record ReorderLedgerSummaryBoxesCommand : IRequest
{
    public Guid LedgerId { get; init; }

    public IReadOnlyList<Guid> OrderedBoxIds { get; init; } = Array.Empty<Guid>();
}

public class ReorderLedgerSummaryBoxesCommandHandler : IRequestHandler<ReorderLedgerSummaryBoxesCommand>
{
    private readonly IApplicationDbContext _context;

    public ReorderLedgerSummaryBoxesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReorderLedgerSummaryBoxesCommand request, CancellationToken cancellationToken)
    {
        var boxes = await _context.LedgerSummaryBoxes
            .Where(b => b.LedgerId == request.LedgerId)
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        for (var i = 0; i < request.OrderedBoxIds.Count; i++)
        {
            if (boxes.TryGetValue(request.OrderedBoxIds[i], out var box))
            {
                box.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
