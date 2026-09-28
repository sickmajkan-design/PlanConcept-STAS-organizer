using Construction.Application.Common.Interfaces;
using Construction.Application.Features.VehicleTolls.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.VehicleTolls.Queries.GetVehicleTolls;

/// <summary>All tolls — vignettes, tunnels, road passages — carried by one vehicle.</summary>
/// <remarks>
/// Not paged, same reasoning as <c>GetAttachmentsQuery</c>: a vehicle's
/// tolls are counted in single digits.
/// </remarks>
public record GetVehicleTollsQuery(Guid VehicleId) : IRequest<IReadOnlyList<VehicleTollDto>>;

public class GetVehicleTollsQueryValidator : AbstractValidator<GetVehicleTollsQuery>
{
    public GetVehicleTollsQueryValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
    }
}

public class GetVehicleTollsQueryHandler
    : IRequestHandler<GetVehicleTollsQuery, IReadOnlyList<VehicleTollDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehicleTollsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<VehicleTollDto>> Handle(
        GetVehicleTollsQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var tolls = await _context.VehicleTolls
            .AsNoTracking()
            .Include(t => t.PaidByUser)
            .Where(t => t.VehicleId == request.VehicleId)
            .ToListAsync(cancellationToken);

        var dtos = tolls.Select(t => VehicleTollMapping.ToDto(t, today));

        // Expired and expiring-soon first (whoever is about to drive needs
        // to see those before anything else), then soonest ValidUntil, then
        // unpaid rows last — they have no ValidUntil at all, so they would
        // otherwise sort ahead of everything on a null-first ascending order.
        return dtos
            .OrderBy(d => d.ComputedState switch
            {
                "Expired" => 0,
                "ExpiringSoon" => 1,
                "Paid" => 2,
                _ => 3
            })
            .ThenBy(d => d.ValidUntil ?? DateOnly.MaxValue)
            .ToList();
    }
}
