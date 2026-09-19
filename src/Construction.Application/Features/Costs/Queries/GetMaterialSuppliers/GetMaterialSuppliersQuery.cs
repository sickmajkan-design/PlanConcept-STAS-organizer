using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetMaterialSuppliers;

/// <summary>
/// The suppliers already typed on deliveries, most recently used first, so the
/// next delivery can pick one instead of retyping it slightly differently.
/// </summary>
public record GetMaterialSuppliersQuery : IRequest<IReadOnlyList<string>>;

public class GetMaterialSuppliersQueryHandler
    : IRequestHandler<GetMaterialSuppliersQuery, IReadOnlyList<string>>
{
    private const int MaxSuggestions = 50;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMaterialSuppliersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<string>> Handle(
        GetMaterialSuppliersQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see stock movements.");
        }

        return await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.Supplier != null)
            .GroupBy(m => m.Supplier!)
            .OrderByDescending(g => g.Max(m => m.OccurredOn))
            .Select(g => g.Key)
            .Take(MaxSuggestions)
            .ToListAsync(cancellationToken);
    }
}
