using System.Net;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Outbox;
using Construction.Application.Features.StorageCleanup;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SystemStatus;

/// <summary>The kinds of server trouble a Super Admin can be told about, and what each does until they choose.</summary>
public static class ServerAlertKeys
{
    public const string Disk = "disk";
    public const string Outage = "outage";
    public const string BackupMissing = "backupMissing";
    public const string BackupOld = "backupOld";
    public const string RestoreNotChecked = "restoreNotChecked";
    public const string ScannerDown = "scannerDown";
    public const string OutboxAbandoned = "outboxAbandoned";
    public const string Certificate = "certificate";

    /// <summary>In the order the settings are shown.</summary>
    public static readonly string[] All =
        [Disk, Outage, BackupMissing, BackupOld, ScannerDown, OutboxAbandoned, Certificate, RestoreNotChecked];

    /// <summary>
    /// What a kind does before anybody changes it. The restore rehearsal is off: until the first Sunday it has never run,
    /// and a notice about that would be noise.
    /// </summary>
    public static (bool Email, bool Push) Default(string key) => key switch
    {
        Disk or Outage => (true, true),
        BackupMissing or BackupOld or ScannerDown or Certificate => (true, false),
        OutboxAbandoned => (false, true),
        _ => (false, false),
    };

    public static bool IsKnown(string key) => Array.IndexOf(All, key) >= 0;
}

public static class ServerAlertPolicy
{
    /// <summary>While a problem lasts it is repeated this often, not more.</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(6);

    public static bool CooldownPassed(DateTime? lastSentAt, DateTime now) => CooldownPassed(lastSentAt, now, Cooldown);

    public static bool CooldownPassed(DateTime? lastSentAt, DateTime now, TimeSpan cooldown) => lastSentAt is null || now - lastSentAt.Value >= cooldown;

    /// <summary>A certificate is a problem for two weeks, so it is mentioned once a day, not four times.</summary>
    public static TimeSpan CooldownFor(string key) => key == ServerAlertKeys.Certificate ? TimeSpan.FromHours(24) : Cooldown;
}

/// <summary>Delivers a server notice to the Super Admins through the channels chosen for its kind.</summary>
public class ServerAlertSender
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IOutbox _outbox;
    private readonly IDateTimeProvider _clock;

    public ServerAlertSender(IApplicationDbContext context, INotificationService notifications, IOutbox outbox, IDateTimeProvider clock)
    {
        _context = context;
        _notifications = notifications;
        _outbox = outbox;
        _clock = clock;
    }

    public async Task<ServerAlertSetting> GetOrCreateAsync(string key, CancellationToken cancellationToken)
    {
        // Local first: a row added earlier in this unit of work is not in the database yet.
        var setting = _context.ServerAlertSettings.Local.FirstOrDefault(s => s.Key == key)
            ?? await _context.ServerAlertSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is null)
        {
            var (email, push) = ServerAlertKeys.Default(key);
            setting = new ServerAlertSetting { Id = Guid.NewGuid(), Key = key, EmailEnabled = email, PushEnabled = push };
            _context.ServerAlertSettings.Add(setting);
        }

        return setting;
    }

    /// <returns>How many Super Admins were told; zero when the kind is off or was sent too recently.</returns>
    public async Task<int> SendAsync(
        string key,
        string title,
        string body,
        CancellationToken cancellationToken,
        IReadOnlyCollection<Guid>? recipients = null,
        bool bypassCooldown = false)
    {
        var setting = await GetOrCreateAsync(key, cancellationToken);
        var now = _clock.UtcNow;

        if ((!setting.EmailEnabled && !setting.PushEnabled) || (!bypassCooldown && !ServerAlertPolicy.CooldownPassed(setting.LastSentAt, now, ServerAlertPolicy.CooldownFor(key))))
        {
            await _context.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var admins = await _context.Users
            .Where(u => u.IsActive && u.Role == UserRole.SuperAdmin && (recipients == null || recipients.Contains(u.Id)))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        if (admins.Count == 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return 0;
        }

        if (setting.PushEnabled)
        {
            await _notifications.NotifyUsersAsync(
                admins.Select(a => a.Id).ToList(), NotificationType.GeneralAnnouncement, title, body, cancellationToken: cancellationToken);
        }

        if (setting.EmailEnabled)
        {
            foreach (var admin in admins)
            {
                _outbox.Enqueue(new EmailPayload(admin.Email, title, Html(title, body)));
            }
        }

        setting.LastSentAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        return admins.Count;
    }

    /// <summary>One test notice to one person, by both channels, whatever the settings say.</summary>
    public async Task SendTestAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        const string title = "Probno obavještenje o serveru";
        const string body = "Ovako izgleda obavještenje o serveru. Ako ga vidite, i push i e-pošta rade.";

        await _notifications.NotifyUserAsync(userId, NotificationType.GeneralAnnouncement, title, body, cancellationToken: cancellationToken);
        _outbox.Enqueue(new EmailPayload(email, title, Html(title, body)));
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string Html(string title, string body) =>
        $"<h3>{WebUtility.HtmlEncode(title)}</h3><p>{WebUtility.HtmlEncode(body)}</p>";
}

public class ServerAlertSettingDto
{
    public string Key { get; init; } = string.Empty;

    public bool EmailEnabled { get; init; }

    public bool PushEnabled { get; init; }

    public DateTime? LastSentAtUtc { get; init; }
}

public record GetServerAlertSettingsQuery : IRequest<List<ServerAlertSettingDto>>;

public class GetServerAlertSettingsQueryHandler : IRequestHandler<GetServerAlertSettingsQuery, List<ServerAlertSettingDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetServerAlertSettingsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ServerAlertSettingDto>> Handle(GetServerAlertSettingsQuery request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        var rows = await _context.ServerAlertSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, cancellationToken);

        return ServerAlertKeys.All.Select(key =>
        {
            var (email, push) = ServerAlertKeys.Default(key);
            rows.TryGetValue(key, out var row);

            return new ServerAlertSettingDto
            {
                Key = key,
                EmailEnabled = row?.EmailEnabled ?? email,
                PushEnabled = row?.PushEnabled ?? push,
                LastSentAtUtc = row?.LastSentAt,
            };
        }).ToList();
    }
}

public record UpdateServerAlertSettingCommand(string Key, bool EmailEnabled, bool PushEnabled) : IRequest;

public class UpdateServerAlertSettingCommandHandler : IRequestHandler<UpdateServerAlertSettingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ServerAlertSender _sender;

    public UpdateServerAlertSettingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ServerAlertSender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task Handle(UpdateServerAlertSettingCommand request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        if (!ServerAlertKeys.IsKnown(request.Key))
        {
            throw new NotFoundException("ServerAlert", request.Key);
        }

        var setting = await _sender.GetOrCreateAsync(request.Key, cancellationToken);
        setting.EmailEnabled = request.EmailEnabled;
        setting.PushEnabled = request.PushEnabled;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record SendTestServerAlertCommand : IRequest;

public class SendTestServerAlertCommandHandler : IRequestHandler<SendTestServerAlertCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ServerAlertSender _sender;

    public SendTestServerAlertCommandHandler(ICurrentUserService currentUser, ServerAlertSender sender)
    {
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task Handle(SendTestServerAlertCommand request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        if (_currentUser.UserId is { } userId && _currentUser.Email is { } email)
        {
            await _sender.SendTestAsync(userId, email, cancellationToken);
        }
    }
}

/// <summary>
/// Looks at what is wrong with the server right now and tells the Super Admins by the channels they chose. The disk has
/// its own daily command (<see cref="SendDiskSpaceWarningCommand"/>); the rest are here. Meant to run every few minutes.
/// </summary>
public record SendServerAlertsCommand : IRequest<int>;

public class SendServerAlertsCommandHandler : IRequestHandler<SendServerAlertsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IBackupCatalog _backups;
    private readonly IDockerStatusReader _docker;
    private readonly IVirusScanner _scanner;
    private readonly ServerAlertSender _sender;
    private readonly IDateTimeProvider _clock;

    public SendServerAlertsCommandHandler(
        IApplicationDbContext context,
        IBackupCatalog backups,
        IDockerStatusReader docker,
        IVirusScanner scanner,
        ServerAlertSender sender,
        IDateTimeProvider clock)
    {
        _context = context;
        _backups = backups;
        _docker = docker;
        _scanner = scanner;
        _sender = sender;
        _clock = clock;
    }

    public async Task<int> Handle(SendServerAlertsCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var sent = 0;

        var backups = _backups.Read();

        if (backups.Known)
        {
            var newest = backups.Items.FirstOrDefault();

            if (newest is null)
            {
                sent += await _sender.SendAsync(ServerAlertKeys.BackupMissing, "Nema rezervnih kopija",
                    "Na serveru nema nijedne rezervne kopije baze i dokumenata.", cancellationToken);
            }
            else if (now - newest.TakenAtUtc > TimeSpan.FromHours(26))
            {
                sent += await _sender.SendAsync(ServerAlertKeys.BackupOld, "Noćna rezervna kopija je izostala",
                    $"Poslednja rezervna kopija je napravljena {newest.TakenAtUtc:dd.MM.yyyy. HH:mm} (UTC), prije više od 26 sati.", cancellationToken);
            }

            if (backups.RestoreCheckedAtUtc is null || now - backups.RestoreCheckedAtUtc > TimeSpan.FromDays(10))
            {
                sent += await _sender.SendAsync(ServerAlertKeys.RestoreNotChecked, "Povratak iz kopije nije provjeren",
                    "Više od 10 dana nije uspješno probano vraćanje iz rezervne kopije, pa se ne zna da li se kopija može vratiti.", cancellationToken);
            }
        }

        if (_scanner.IsEnabled && !await _scanner.IsReachableAsync(cancellationToken))
        {
            sent += await _sender.SendAsync(ServerAlertKeys.ScannerDown, "Antivirus ne radi",
                "Antivirus servis ne odgovara, pa je slanje dokumenata zaustavljeno dok se ne vrati.", cancellationToken);
        }

        if (_docker.Read().Certificate is { } certificate && certificate.NotAfterUtc - now < TimeSpan.FromDays(14))
        {
            var days = (int)Math.Floor((certificate.NotAfterUtc - now).TotalDays);

            sent += await _sender.SendAsync(
                ServerAlertKeys.Certificate,
                days < 0 ? "HTTPS certifikat je istekao" : "HTTPS certifikat uskoro ističe",
                days < 0
                    ? $"Certifikat je istekao {certificate.NotAfterUtc:dd.MM.yyyy.}, pa se aplikacija ne može otvoriti bez upozorenja u pregledniku. Provjerite Caddy."
                    : $"Certifikat ističe za {days} dana ({certificate.NotAfterUtc:dd.MM.yyyy.}). Caddy ga obično obnavlja sam; ako se ovo ponavlja, provjerite Caddy.",
                cancellationToken);
        }

        var abandonedSince = now.AddHours(-24);

        if (await _context.OutboxMessages.AsNoTracking().AnyAsync(m => m.AbandonedAt >= abandonedSince, cancellationToken))
        {
            sent += await _sender.SendAsync(ServerAlertKeys.OutboxAbandoned, "Poruke se ne šalju",
                "Neke poruke (e-pošta, obavještenja) su odbačene poslije ponovljenih grešaka. Provjerite servis za slanje.", cancellationToken);
        }

        // Reported once, when the application is back: only then is the length of the gap known.
        var outage = await _context.ServiceOutages.AsNoTracking()
            .Where(o => o.EndedAt >= now.AddMinutes(-30))
            .OrderByDescending(o => o.EndedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (outage is not null)
        {
            var setting = await _sender.GetOrCreateAsync(ServerAlertKeys.Outage, cancellationToken);

            if (setting.LastSentAt is null || setting.LastSentAt < outage.EndedAt)
            {
                var minutes = (int)Math.Round((outage.EndedAt - outage.StartedAt).TotalMinutes);

                sent += await _sender.SendAsync(ServerAlertKeys.Outage, "Server nije bio dostupan",
                    $"Aplikacija nije radila oko {minutes} min (od {outage.StartedAt:dd.MM. HH:mm} do {outage.EndedAt:HH:mm} UTC) i ponovo radi.",
                    cancellationToken, bypassCooldown: true);
            }
        }

        return sent;
    }
}
