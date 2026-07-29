using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Commands.SendAnnouncement;

/// <summary>
/// Sends a general announcement to all active users, optionally narrowed to
/// one role and/or the crew of one project.
/// </summary>
public record SendAnnouncementCommand : IRequest<int>
{
    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;

    /// <summary>Limit the audience to one role.</summary>
    public UserRole? Role { get; init; }

    /// <summary>Limit the audience to users whose employee is assigned to this project.</summary>
    public Guid? ProjectId { get; init; }
}

public class SendAnnouncementCommandValidator : AbstractValidator<SendAnnouncementCommand>
{
    public SendAnnouncementCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(256);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(4000);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role is not a valid value.")
            .When(x => x.Role is not null);
    }
}

public class SendAnnouncementCommandHandler : IRequestHandler<SendAnnouncementCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public SendAnnouncementCommandHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<int> Handle(SendAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var users = _context.Users.Where(u => u.IsActive);

        if (request.Role is { } role)
        {
            users = users.Where(u => u.Role == role);
        }

        if (request.ProjectId is { } projectId)
        {
            users = users.Where(u =>
                u.EmployeeId != null &&
                _context.EmployeeProjects.Any(ep =>
                    ep.ProjectId == projectId && ep.EmployeeId == u.EmployeeId));
        }

        var userIds = await users.Select(u => u.Id).ToListAsync(cancellationToken);

        return await _notificationService.NotifyUsersAsync(
            userIds,
            NotificationType.GeneralAnnouncement,
            request.Title.Trim(),
            request.Body.Trim(),
            data: null,
            cancellationToken);
    }
}
