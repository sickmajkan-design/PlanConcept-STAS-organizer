using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateMaterialMovement;

/// <summary>Corrects a previously recorded delivery, issue, or correction.</summary>
public record UpdateMaterialMovementCommand : IRequest<MaterialMovementDto>
{
    public Guid Id { get; init; }

    public Guid MaterialId { get; init; }

    public MaterialMovementKind Kind { get; init; }

    public decimal Quantity { get; init; }

    public decimal? UnitPrice { get; init; }

    public Guid? ProjectId { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string? Note { get; init; }
}

public class UpdateMaterialMovementCommandValidator
    : AbstractValidator<UpdateMaterialMovementCommand>
{
    public UpdateMaterialMovementCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Id).NotEmpty();
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

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Say which site the material went to.")
            .When(x => x.Kind == MaterialMovementKind.Out);

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("Stock cannot move in the future.")
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A movement cannot be recorded more than {CostRules.MaxBackdatingDays} days back.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateMaterialMovementCommandHandler
    : IRequestHandler<UpdateMaterialMovementCommand, MaterialMovementDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateMaterialMovementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<MaterialMovementDto> Handle(
        UpdateMaterialMovementCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanDeleteSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct recorded movements.");
        }

        var movement = await _context.MaterialMovements
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MaterialMovement), request.Id);

        if (request.MaterialId != movement.MaterialId
            && !await _context.Materials.AnyAsync(m => m.Id == request.MaterialId, cancellationToken))
        {
            throw new NotFoundException(nameof(Material), request.MaterialId);
        }

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        // The stock effect only stays correct if it is undone and reapplied on
        // the same material — moving a movement to a different material would
        // need to reverse on one shelf and apply on another, which is a
        // bigger change than "fix a typo" and is refused instead.
        if (request.MaterialId != movement.MaterialId)
        {
            throw new ConflictException(
                "A movement cannot be moved to a different material; delete it and record a new one instead.");
        }

        var previousSigned = movement.SignedQuantity;

        movement.Kind = request.Kind;
        movement.Quantity = request.Quantity;
        movement.UnitPrice = await ResolveUnitPriceAsync(request, cancellationToken);
        movement.ProjectId = request.ProjectId;
        movement.OccurredOn = request.OccurredOn;
        movement.Note = request.Note?.Trim();

        var delta = movement.SignedQuantity - previousSigned;
        var materialId = movement.MaterialId;

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                var now = _dateTimeProvider.UtcNow;

                var updated = await _context.Materials
                    .Where(m => m.Id == materialId && m.Quantity + delta >= 0)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.Quantity, m => m.Quantity + delta)
                            .SetProperty(m => m.LastUpdated, now)
                            .SetProperty(m => m.UpdatedAt, now),
                        token);

                if (updated == 0)
                {
                    throw new ConflictException(
                        "That correction would put the stock below zero.");
                }

                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        return await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.Id == movement.Id)
            .Select(MaterialMovementMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>Mirrors <c>RecordMaterialMovementCommandHandler.ResolveUnitPriceAsync</c>.</summary>
    private async Task<decimal?> ResolveUnitPriceAsync(
        UpdateMaterialMovementCommand request,
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
