using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs.Models;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetMyFuel;

/// <summary>
/// The vehicles that are signed out to the caller: the ones they may record fuel for.
/// </summary>
/// <remarks>
/// A driver without office or site-management rights cannot list the fleet, and should not be able
/// to; what they need is the one vehicle in their hands. Works for every role, since a foreman who
/// holds a van is in the same position.
/// </remarks>
public record GetMyVehiclesQuery : IRequest<List<VehicleDto>>;

public class GetMyVehiclesQueryHandler : IRequestHandler<GetMyVehiclesQuery, List<VehicleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _clock;

    public GetMyVehiclesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUserService = currentUserService;
        _clock = clock;
    }

    public async Task<List<VehicleDto>> Handle(GetMyVehiclesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.EmployeeId is not { } employeeId)
        {
            return [];
        }

        return await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.AssignedEmployeeId == employeeId)
            .OrderBy(v => v.Brand).ThenBy(v => v.Model)
            .Select(VehicleMapping.Projection(DateOnly.FromDateTime(_clock.UtcNow)))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>The fill-ups the caller recorded themselves, newest first.</summary>
public record GetMyFuelExpensesQuery : IPagedQuery, IRequest<PagedList<VehicleExpenseDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetMyFuelExpensesQueryValidator : PagedQueryValidator<GetMyFuelExpensesQuery>;

public class GetMyFuelExpensesQueryHandler
    : IRequestHandler<GetMyFuelExpensesQuery, PagedList<VehicleExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyFuelExpensesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<VehicleExpenseDto>> Handle(
        GetMyFuelExpensesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Sign in to see your fill-ups.");
        }

        return await PagedList<VehicleExpenseDto>.CreateAsync(
            _context.VehicleExpenses
                .AsNoTracking()
                .Where(e => e.RecordedByUserId == userId && e.Kind == VehicleExpenseKind.Fuel)
                .OrderByDescending(e => e.OccurredOn)
                .ThenByDescending(e => e.CreatedAt)
                .ThenBy(e => e.Id)
                .Select(VehicleExpenseMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
