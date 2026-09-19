using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Queries.GetMaterialPricing;

/// <summary>What a material has actually cost, worked out from its deliveries.</summary>
public class MaterialPricingDto
{
    /// <summary>Price per unit on the most recent priced delivery.</summary>
    public decimal? LastPurchasePrice { get; init; }

    public DateOnly? LastPurchasedOn { get; init; }

    public string? LastSupplier { get; init; }

    /// <summary>Weighted by quantity, over every priced delivery.</summary>
    public decimal? AveragePurchasePrice { get; init; }

    /// <summary>How much has been delivered in all, priced or not.</summary>
    public decimal TotalReceived { get; init; }
}

public record GetMaterialPricingQuery(Guid MaterialId) : IRequest<MaterialPricingDto>;

/// <remarks>
/// A separate query rather than fields on the material: purchase prices are
/// spending records, and the material itself is read by people who may not see
/// spending.
/// </remarks>
public class GetMaterialPricingQueryHandler
    : IRequestHandler<GetMaterialPricingQuery, MaterialPricingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMaterialPricingQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MaterialPricingDto> Handle(
        GetMaterialPricingQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see stock movements.");
        }

        if (!await _context.Materials.AnyAsync(m => m.Id == request.MaterialId, cancellationToken))
        {
            throw new NotFoundException(nameof(Material), request.MaterialId);
        }

        var deliveries = await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.MaterialId == request.MaterialId && m.Kind == MaterialMovementKind.In)
            .Select(m => new { m.Quantity, m.UnitPrice, m.OccurredOn, m.CreatedAt, m.Supplier })
            .ToListAsync(cancellationToken);

        var priced = deliveries.Where(d => d.UnitPrice is not null).ToList();
        var last = priced
            .OrderByDescending(d => d.OccurredOn)
            .ThenByDescending(d => d.CreatedAt)
            .FirstOrDefault();
        var pricedQuantity = priced.Sum(d => d.Quantity);

        return new MaterialPricingDto
        {
            LastPurchasePrice = last?.UnitPrice,
            LastPurchasedOn = last?.OccurredOn,
            LastSupplier = last?.Supplier,
            AveragePurchasePrice = pricedQuantity > 0
                ? Math.Round(priced.Sum(d => d.Quantity * d.UnitPrice!.Value) / pricedQuantity, 2)
                : null,
            TotalReceived = deliveries.Sum(d => d.Quantity)
        };
    }
}
