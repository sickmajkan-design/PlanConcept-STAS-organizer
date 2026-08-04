using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.RecordMaterialMovement;

/// <summary>Records a delivery, an issue to site, or a stocktake correction.</summary>
public record RecordMaterialMovementCommand : IRequest<MaterialMovementDto>
{
    public Guid MaterialId { get; init; }

    public MaterialMovementKind Kind { get; init; }

    /// <summary>Positive for a delivery or an issue; signed for a correction.</summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// Cost per unit. Required on a delivery; on an issue it overrides the
    /// average the system would otherwise work out.
    /// </summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>Which site consumed it. Required when issuing.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? OccurredOn { get; init; }

    public string? Note { get; init; }
}

public class RecordMaterialMovementCommandValidator
    : AbstractValidator<RecordMaterialMovementCommand>
{
    public RecordMaterialMovementCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.MaterialId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Quantity)
            .NotEqual(0).WithMessage("A movement of nothing is not a movement.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("A delivery or an issue is a quantity, not a change; use a correction to take stock down.")
            .When(x => x.Kind != MaterialMovementKind.Adjustment);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitPrice is not null);

        // Issuing stock with no site is how material disappears from the
        // costing report while still leaving the shelf.
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Say which site the material went to.")
            .When(x => x.Kind == MaterialMovementKind.Out);

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("Stock cannot move in the future.")
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A movement cannot be recorded more than {CostRules.MaxBackdatingDays} days back.")
            .When(x => x.OccurredOn is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordMaterialMovementCommandHandler
    : IRequestHandler<RecordMaterialMovementCommand, MaterialMovementDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public RecordMaterialMovementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<MaterialMovementDto> Handle(
        RecordMaterialMovementCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record stock movements.");
        }

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var now = _dateTimeProvider.UtcNow;

        var movement = new MaterialMovement
        {
            MaterialId = request.MaterialId,
            Kind = request.Kind,
            Quantity = request.Quantity,
            UnitPrice = await ResolveUnitPriceAsync(request, cancellationToken),
            ProjectId = request.ProjectId,
            OccurredOn = request.OccurredOn ?? DateOnly.FromDateTime(now),
            Note = request.Note?.Trim(),
            RecordedByUserId = _currentUserService.UserId
        };

        // The running total and the movement have to land together, or the
        // stock screen and the history start disagreeing and there is no way
        // to tell which one is lying.
        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                var delta = movement.SignedQuantity;

                // Conditional update: applies the delta only when the result
                // stays non-negative, so two people issuing the last of a
                // pallet at once cannot drive it below zero between them.
                var updated = await _context.Materials
                    .Where(m => m.Id == request.MaterialId && m.Quantity + delta >= 0)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.Quantity, m => m.Quantity + delta)
                            .SetProperty(m => m.LastUpdated, now)
                            .SetProperty(m => m.UpdatedAt, now),
                        token);

                if (updated == 0)
                {
                    var exists = await _context.Materials
                        .AnyAsync(m => m.Id == request.MaterialId, token);

                    throw exists
                        ? new ConflictException(
                            "That movement would put the stock below zero.")
                        : new NotFoundException(nameof(Material), request.MaterialId);
                }

                _context.MaterialMovements.Add(movement);
                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        return await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.Id == movement.Id)
            .ProjectTo<MaterialMovementDto>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// What a unit of this material is worth for this movement.
    /// </summary>
    /// <remarks>
    /// A delivery brings its own price. An issue is valued at the weighted
    /// average of everything bought so far and that value is stored on the
    /// row, so a later delivery at a different price cannot retroactively
    /// change what a finished job is recorded as having cost. A correction is
    /// left unpriced: nothing consumed it.
    ///
    /// Weighted average rather than FIFO because a heap of gravel has no
    /// batches to consume in order, and FIFO would need a layer table to
    /// answer a question nobody on a building site is asking.
    /// </remarks>
    private async Task<decimal?> ResolveUnitPriceAsync(
        RecordMaterialMovementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Kind == MaterialMovementKind.Adjustment)
        {
            return null;
        }

        if (request.UnitPrice is { } supplied)
        {
            return supplied;
        }

        if (request.Kind == MaterialMovementKind.In)
        {
            // A delivery with no price is allowed — sometimes the invoice
            // follows the lorry — but it then contributes nothing to the
            // average, which is honest: an unknown price is not a zero one.
            return null;
        }

        var purchases = await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.MaterialId == request.MaterialId
                && m.Kind == MaterialMovementKind.In
                && m.UnitPrice != null)
            .GroupBy(m => m.MaterialId)
            .Select(g => new
            {
                Spent = g.Sum(m => m.UnitPrice!.Value * m.Quantity),
                Quantity = g.Sum(m => m.Quantity)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return purchases is { Quantity: > 0 }
            ? purchases.Spent / purchases.Quantity
            : null;
    }
}
