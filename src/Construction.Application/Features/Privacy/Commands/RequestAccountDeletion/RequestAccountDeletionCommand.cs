using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Privacy.Commands.RequestAccountDeletion;

/// <summary>
/// The signed-in person asks for their personal data to be erased. Only tells
/// the Super Admins; the erasure itself stays a deliberate act of
/// <see cref="ErasePersonalData.ErasePersonalDataCommand"/>, never automatic.
/// </summary>
/// <remarks>
/// Accounts here are opened by the firm, so this is a request, not a deletion:
/// hours and pay records have to be kept by law. shortcut: the notice is the only
/// record of the request (no table), so an admin who clears it loses it — add a
/// requests table if requests ever need a status or a deadline.
/// </remarks>
public record RequestAccountDeletionCommand : IRequest;

public class RequestAccountDeletionCommandHandler : IRequestHandler<RequestAccountDeletionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public RequestAccountDeletionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task Handle(RequestAccountDeletionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedException("User is not authenticated.");

        var who = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Name = u.EmployeeId == null ? u.Email : u.Employee!.FirstName + " " + u.Employee.LastName,
                u.EmployeeId
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        var superAdmins = await _context.Users
            .Where(u => u.IsActive && u.Role == UserRole.SuperAdmin && u.Id != userId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (superAdmins.Count == 0)
        {
            return;
        }

        var data = new Dictionary<string, string>();
        if (who.EmployeeId is { } employeeId)
        {
            data["employeeId"] = employeeId.ToString();
        }

        await _notifications.NotifyUsersAsync(
            superAdmins,
            NotificationType.GeneralAnnouncement,
            "Zahtjev za brisanje naloga",
            $"{who.Name} traži brisanje svojih ličnih podataka. Otvorite radnika i pokrenite brisanje (samo Super Admin).",
            data,
            cancellationToken: cancellationToken);
    }
}
