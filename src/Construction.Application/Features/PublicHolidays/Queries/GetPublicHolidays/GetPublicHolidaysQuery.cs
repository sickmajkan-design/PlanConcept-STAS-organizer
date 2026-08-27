using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.PublicHolidays.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.PublicHolidays.Queries.GetPublicHolidays;

/// <summary>Every public holiday on file, soonest first.</summary>
/// <remarks>
/// Not paged. The list is a handful of rows a year — paging it would cost a
/// second request to learn there is no second page.
/// </remarks>
public record GetPublicHolidaysQuery : IRequest<IReadOnlyList<PublicHolidayDto>>
{
    /// <summary>Narrows to one calendar year, when given.</summary>
    public int? Year { get; init; }
}

public class GetPublicHolidaysQueryHandler
    : IRequestHandler<GetPublicHolidaysQuery, IReadOnlyList<PublicHolidayDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPublicHolidaysQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<PublicHolidayDto>> Handle(
        GetPublicHolidaysQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeLabourCost(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see the holiday calendar.");
        }

        var query = _context.PublicHolidays.AsNoTracking();

        if (request.Year is { } year)
        {
            query = query.Where(h => h.Date.Year == year);
        }

        return await query
            .OrderBy(h => h.Date)
            .Select(PublicHolidayMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
