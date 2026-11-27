using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>
/// Reminds the people posted on a day who have not yet confirmed their posting to look at it and confirm. One notice per
/// person, however many postings they have.
/// </summary>
public record RemindUnconfirmedPostingsCommand : IRequest<int>
{
    public DateOnly Date { get; init; }
}

public class RemindUnconfirmedPostingsCommandHandler : IRequestHandler<RemindUnconfirmedPostingsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public RemindUnconfirmedPostingsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    /// <returns>How many people were reminded.</returns>
    public async Task<int> Handle(RemindUnconfirmedPostingsCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");

        var day = request.Date;
        var token = access.IsScoped ? BranchTree.Token(access.BranchId!.Value) : string.Empty;

        var userIds = await _context.EmployeeProjects.AsNoTracking()
            .Where(ep => ep.StartDate <= day && (ep.EndDate == null || ep.EndDate >= day) && ep.AcknowledgedAt == null)
            .Where(ep => ep.Employee.Status == EmployeeStatus.Active && ep.Employee.User != null && ep.Employee.User.IsActive)
            .Where(ep => !_context.Absences.Any(a => a.EmployeeId == ep.EmployeeId && a.Status == AbsenceStatus.Approved && a.StartDate <= day && a.EndDate >= day))
            .Where(ep => !access.IsScoped || ep.Employee.BranchPeriods.Any(b => b.Branch.Path.Contains(token) && b.EndDate == null))
            .Select(ep => ep.Employee.User!.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return 0;
        }

        var data = new Dictionary<string, string> { ["reminder"] = "schedule", ["date"] = day.ToString("yyyy-MM-dd") };

        await _notifications.NotifyUsersAsync(
            userIds,
            NotificationType.ProjectAssigned,
            "Please confirm your schedule",
            $"Your schedule for {day:dd.MM.yyyy} is waiting for you to confirm it.",
            data,
            cancellationToken: cancellationToken);

        return userIds.Count;
    }
}
