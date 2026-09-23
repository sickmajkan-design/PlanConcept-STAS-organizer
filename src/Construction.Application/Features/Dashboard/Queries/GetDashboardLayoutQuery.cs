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

        // A layout saved before the board became free-form (position/size
        // used to be an implicit column+order pair) deserializes with
        // W/H defaulted to 0 — System.Text.Json leaves properties missing
        // from the old JSON at their CLR default rather than throwing. That
        // 0 is unusable as a grid span, so re-arrange those widgets with the
        // same auto-packing the first-visit default uses, keeping the set
        // and order of widgets the user actually chose.
        if (widgets.Any(w => w.W <= 0 || w.H <= 0))
        {
            widgets = AutoArrange(widgets.Select(w => w.Type).ToList());
        }

        return new DashboardLayoutDto { Widgets = widgets };
    }

    private const int DefaultWidth = 6;
    private const int DefaultHeight = 12;

    /// <summary>
    /// The two-column-looking board a first visit (or a pre-freeform-grid
    /// migration) lands on. <see cref="DashboardWidgetTypes.CompanyKpi"/>
    /// leads the left column — the one glance that answers "how's the
    /// company doing" — with everything else alternating columns and
    /// stacking under whichever side is currently shorter, so a wide screen
    /// isn't left with one tall stack and an empty half. It's just a starting
    /// arrangement: the user can drag and resize every widget from here.
    /// </summary>
    private static List<DashboardWidgetDto> DefaultWidgets() => AutoArrange(DashboardWidgetTypes.All);

    private static List<DashboardWidgetDto> AutoArrange(IReadOnlyList<string> types)
    {
        var columnHeights = new[] { 0, 0 };

        return types
            .Select((type, index) =>
            {
                var column = type == DashboardWidgetTypes.CompanyKpi ? 0 : index % 2;
                var y = columnHeights[column];
                columnHeights[column] += DefaultHeight;

                return new DashboardWidgetDto
                {
                    Id = Guid.NewGuid(),
                    Type = type,
                    X = column * DefaultWidth,
                    Y = y,
                    W = DefaultWidth,
                    H = DefaultHeight,
                };
            })
            .ToList();
    }
}
