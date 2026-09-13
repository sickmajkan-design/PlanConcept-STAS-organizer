using System.Text.Json;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Dashboard.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Dashboard.Queries;

/// <summary>
/// The signed-in user's own dashboard layout. A user with no saved layout
/// yet — first visit — gets the default catalog rather than an empty board,
/// so they don't land on a blank home page.
/// </summary>
public record GetDashboardLayoutQuery : IRequest<DashboardLayoutDto>;

public class GetDashboardLayoutQueryHandler
    : IRequestHandler<GetDashboardLayoutQuery, DashboardLayoutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardLayoutQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardLayoutDto> Handle(
        GetDashboardLayoutQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("No signed-in user.");

        var json = await _context.DashboardLayouts
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => d.WidgetsJson)
            .FirstOrDefaultAsync(cancellationToken);

        var widgets = json is null
            ? DefaultWidgets()
            : JsonSerializer.Deserialize<List<DashboardWidgetDto>>(json) ?? DefaultWidgets();

        return new DashboardLayoutDto { Widgets = widgets };
    }

    /// <summary>
    /// The two-column board a first visit lands on. <see cref="DashboardWidgetTypes.CompanyKpi"/>
    /// leads the left column — the one glance that answers "how's the
    /// company doing" — with everything else alternating columns so a wide
    /// screen isn't left with one tall stack and an empty half.
    /// </summary>
    private static List<DashboardWidgetDto> DefaultWidgets()
    {
        var columnOrders = new[] { 0, 0 };

        return DashboardWidgetTypes.All
            .Select((type, index) =>
            {
                var column = type == DashboardWidgetTypes.CompanyKpi ? 0 : index % 2;
                var order = columnOrders[column]++;

                return new DashboardWidgetDto
                {
                    Id = Guid.NewGuid(),
                    Type = type,
                    Column = column,
                    Order = order,
                };
            })
            .ToList();
    }
}
