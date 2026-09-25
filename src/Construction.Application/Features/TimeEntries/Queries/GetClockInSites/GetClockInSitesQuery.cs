using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using MediatR;

namespace Construction.Application.Features.TimeEntries.Queries.GetClockInSites;

/// <summary>One site the employee may clock in to today.</summary>
public record ClockInSiteDto(Guid Id, string Name);

/// <summary>
/// The running sites the signed-in employee is posted to today.
/// </summary>
/// <remarks>
/// Asked by the phone when someone taps "clock in": with more than one, the worker says which
/// site they are on, since neither the phone nor the server can know. With one, or none, the
/// server places the shift itself and the phone need not ask.
/// </remarks>
public record GetClockInSitesQuery : IRequest<IReadOnlyList<ClockInSiteDto>>;

public class GetClockInSitesQueryHandler
    : IRequestHandler<GetClockInSitesQuery, IReadOnlyList<ClockInSiteDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetClockInSitesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<ClockInSiteDto>> Handle(
        GetClockInSitesQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "Only accounts linked to an employee can record work time.");

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var sites = await TimeEntryRules.RunningPostingsAsync(
            _context, employeeId, today, cancellationToken);

        return sites.Select(s => new ClockInSiteDto(s.Id, s.Name)).ToList();
    }
}
