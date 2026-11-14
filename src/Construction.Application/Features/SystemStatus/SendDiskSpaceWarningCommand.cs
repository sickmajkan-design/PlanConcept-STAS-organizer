using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SystemStatus;

/// <summary>
/// Tells the Super Admins, once a day, while the disk is low or close to out. Sends nothing while there is room.
/// </summary>
/// <remarks>
/// Nobody looks at a disk until it is full, so the notice finds them (it reaches the phone as a push as well). The day's
/// notice is not repeated: a Super Admin who already has one from the last 20 hours is skipped, which also keeps two
/// running instances from telling anyone twice.
/// </remarks>
public record SendDiskSpaceWarningCommand : IRequest<int>;

public class SendDiskSpaceWarningCommandHandler : IRequestHandler<SendDiskSpaceWarningCommand, int>
{
    public const string LowTitle = "Malo prostora na serveru";
    public const string CriticalTitle = "Gotovo da nema prostora na serveru";

    private readonly IApplicationDbContext _context;
    private readonly IDiskSpace _disk;
    private readonly ServerAlertSender _sender;
    private readonly IDateTimeProvider _clock;

    public SendDiskSpaceWarningCommandHandler(
        IApplicationDbContext context,
        IDiskSpace disk,
        ServerAlertSender sender,
        IDateTimeProvider clock)
    {
        _context = context;
        _disk = disk;
        _sender = sender;
        _clock = clock;
    }

    public async Task<int> Handle(SendDiskSpaceWarningCommand request, CancellationToken cancellationToken)
    {
        if (_disk.Read() is not { Level: not DiskSpaceLevel.Ok } reading)
        {
            return 0;
        }

        var critical = reading.Level == DiskSpaceLevel.Critical;
        var title = critical ? CriticalTitle : LowTitle;
        var since = _clock.UtcNow.AddHours(-20);

        var alreadyTold = _context.Notifications
            .Where(n => (n.Title == LowTitle || n.Title == CriticalTitle) && n.CreatedAt >= since)
            .Select(n => n.UserId);

        var recipients = await _context.Users
            .Where(u => u.IsActive && u.Role == UserRole.SuperAdmin && !alreadyTold.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            return 0;
        }

        var body = $"Slobodno je {reading.Describe()}. "
            + (critical
                ? "Ako se disk napuni, prestaju slanje dokumenata i rad baze. Očistite prostor ili povećajte disk odmah."
                : "Očistite stare Docker slike i nepotrebne datoteke ili povećajte disk prije nego se napuni.");

        // Its own 20-hour rule above decides when; the channels (push, e-mail) are the Super Admin's choice.
        return await _sender.SendAsync(ServerAlertKeys.Disk, title, body, cancellationToken, recipients, bypassCooldown: true);
    }
}
