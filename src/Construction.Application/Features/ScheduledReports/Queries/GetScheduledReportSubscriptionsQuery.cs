using Construction.Application.Common.Interfaces;
using Construction.Application.Features.ScheduledReports.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.ScheduledReports.Queries;

/// <summary>
/// The signed-in user's own scheduled reports. Not admin-wide: these are
/// personal standing orders ("email me..."), not a shared list to manage on
/// someone else's behalf.
/// </summary>
public record GetScheduledReportSubscriptionsQuery : IRequest<IReadOnlyList<ScheduledReportSubscriptionDto>>;

public class GetScheduledReportSubscriptionsQueryHandler
    : IRequestHandler<GetScheduledReportSubscriptionsQuery, IReadOnlyList<ScheduledReportSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetScheduledReportSubscriptionsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ScheduledReportSubscriptionDto>> Handle(
        GetScheduledReportSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.ScheduledReportSubscriptions
            .AsNoTracking()
            .Where(s => s.CreatedByUserId == _currentUserService.UserId)
            .OrderBy(s => s.NextRunAtUtc)
            .Select(s => new ScheduledReportSubscriptionDto
            {
                Id = s.Id,
                RecipientEmail = s.RecipientEmail,
                ReportType = s.ReportType,
                Cadence = s.Cadence,
                DayOfWeek = s.DayOfWeek,
                DayOfMonth = s.DayOfMonth,
                Language = s.Language,
                NextRunAtUtc = s.NextRunAtUtc,
                CreatedByEmail = s.CreatedByUser.Email,
            })
            .ToListAsync(cancellationToken);
    }
}
