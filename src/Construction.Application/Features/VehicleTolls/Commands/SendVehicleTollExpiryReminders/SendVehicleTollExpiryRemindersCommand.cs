using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.VehicleTolls.Commands.SendVehicleTollExpiryReminders;

/// <summary>
/// Tells each admin about vehicle tolls (vignettes, tunnels, road passages)
/// lapsing within the window they've personally asked for. Same shape as
/// <c>SendExpiryRemindersCommand</c> — see its remarks for why this is a
/// command rather than logic living directly in the hosted service.
/// </summary>
public record SendVehicleTollExpiryRemindersCommand : IRequest<int>
{
    /// <summary>
    /// What an admin gets who has never set <see cref="User.DocumentExpiryReminderDays"/>.
    /// Same default as the document-expiry sweep, and the same field — a
    /// toll is a document-adjacent compliance deadline, not a separate
    /// setting an admin would want to tune independently.
    /// </summary>
    public const int DefaultReminderDays = 30;
}

public class SendVehicleTollExpiryRemindersCommandHandler
    : IRequestHandler<SendVehicleTollExpiryRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendVehicleTollExpiryRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(
        SendVehicleTollExpiryRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var admins = await _context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => new { u.Id, u.DocumentExpiryReminderDays })
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var admin in admins)
        {
            var days = admin.DocumentExpiryReminderDays
                ?? SendVehicleTollExpiryRemindersCommand.DefaultReminderDays;
            var cutoff = today.AddDays(days);

            var due = await _context.VehicleTolls
                .Where(t => t.Status == VehicleTollStatus.Paid
                    && t.ValidUntil != null
                    && t.ValidUntil <= cutoff)
                // Not yet claimed for this admin against this toll's current
                // validity — a renewal changes ValidUntil, which is a fresh
                // claim, so a previous period's claim never suppresses it.
                .Where(t => !_context.VehicleTollExpiryReminders
                    .Any(r => r.VehicleTollId == t.Id
                        && r.UserId == admin.Id
                        && r.ValidUntil == t.ValidUntil))
                .OrderBy(t => t.ValidUntil)
                .Select(t => new
                {
                    t.Id,
                    t.VehicleId,
                    t.Type,
                    t.Country,
                    t.RouteSegment,
                    t.ValidUntil,
                    VehicleRegistrationNumber = t.Vehicle.RegistrationNumber
                })
                .ToListAsync(cancellationToken);

            foreach (var toll in due)
            {
                // The claim row itself is the dedup, exactly like
                // SendExpiryRemindersCommand's AttachmentExpiryReminder claim:
                // a second sweep hitting the unique index fails the insert
                // and skips notifying rather than racing a check-then-set.
                var claim = new VehicleTollExpiryReminder
                {
                    VehicleTollId = toll.Id,
                    UserId = admin.Id,
                    ValidUntil = toll.ValidUntil!.Value,
                    SentAt = now
                };
                _context.VehicleTollExpiryReminders.Add(claim);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    _context.VehicleTollExpiryReminders.Remove(claim);
                    continue;
                }

                var expired = toll.ValidUntil < today;

                await _notifications.NotifyUserAsync(
                    admin.Id,
                    NotificationType.VehicleTollExpiring,
                    expired ? "Vehicle toll has expired" : "Vehicle toll expiring soon",
                    $"{toll.VehicleRegistrationNumber} — {toll.Type} ({toll.Country}" +
                    (string.IsNullOrWhiteSpace(toll.RouteSegment) ? "" : $", {toll.RouteSegment}") +
                    $") ({toll.ValidUntil:dd.MM.yyyy})",
                    new Dictionary<string, string>
                    {
                        ["vehicleTollId"] = toll.Id.ToString(),
                        ["vehicleId"] = toll.VehicleId.ToString(),
                        ["vehicleRegistrationNumber"] = toll.VehicleRegistrationNumber,
                        ["type"] = toll.Type.ToString(),
                        ["country"] = toll.Country,
                        ["validUntil"] = toll.ValidUntil!.Value.ToString("yyyy-MM-dd"),
                        ["expired"] = expired ? "true" : "false"
                    },
                    cancellationToken: cancellationToken);

                sent++;
            }
        }

        return sent;
    }
}
