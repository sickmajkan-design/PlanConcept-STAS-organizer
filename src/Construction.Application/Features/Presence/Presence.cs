using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Presence;

/// <summary>How recent a signal has to be for an account to count as online.</summary>
public static class PresenceWindow
{
    public static readonly TimeSpan Online = TimeSpan.FromMinutes(2);
}

public static class PresenceClient
{
    /// <summary>Browsers identify as Mozilla; the mobile app does not.</summary>
    public static string FromUserAgent(string? userAgent) =>
        userAgent?.Contains("Mozilla", StringComparison.OrdinalIgnoreCase) == true ? "web" : "app";
}

public class OnlineUserDto
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = null!;

    public string Email { get; init; } = null!;

    public UserRole Role { get; init; }

    /// <summary>"web" for the admin panel, "app" for the mobile app.</summary>
    public string Client { get; init; } = null!;

    /// <summary>The screen they are on, for the panel; the app does not report one.</summary>
    public string? Screen { get; init; }

    public DateTime LastSeenAt { get; init; }

    /// <summary>When their current sign-in started, when known.</summary>
    public DateTime? SignedInAt { get; init; }
}

/// <summary>Who is online right now. SuperAdmin accounts are never in the list.</summary>
public record GetOnlineUsersQuery : IRequest<List<OnlineUserDto>>;

public class GetOnlineUsersQueryHandler : IRequestHandler<GetOnlineUsersQuery, List<OnlineUserDto>>
{
    private readonly IPresenceTracker _tracker;
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public GetOnlineUsersQueryHandler(
        IPresenceTracker tracker, IApplicationDbContext context, IDateTimeProvider clock)
    {
        _tracker = tracker;
        _context = context;
        _clock = clock;
    }

    public async Task<List<OnlineUserDto>> Handle(
        GetOnlineUsersQuery request, CancellationToken cancellationToken)
    {
        var online = _tracker.Online(_clock.UtcNow - PresenceWindow.Online);

        if (online.Count == 0)
        {
            return [];
        }

        var ids = online.Select(o => o.UserId).ToList();

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id) && u.Role != UserRole.SuperAdmin)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                FirstName = u.Employee != null ? u.Employee.FirstName : null,
                LastName = u.Employee != null ? u.Employee.LastName : null,
            })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var sessions = await _context.UserSessions
            .AsNoTracking()
            .Where(s => ids.Contains(s.UserId))
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, StartedAt = g.Max(s => s.StartedAt) })
            .ToDictionaryAsync(s => s.UserId, s => s.StartedAt, cancellationToken);

        return online
            .Where(o => users.ContainsKey(o.UserId))
            .Select(o =>
            {
                var user = users[o.UserId];
                var name = $"{user.FirstName} {user.LastName}".Trim();

                return new OnlineUserDto
                {
                    UserId = o.UserId,
                    Name = name.Length > 0 ? name : user.Email,
                    Email = user.Email,
                    Role = user.Role,
                    Client = o.Client,
                    Screen = o.Screen,
                    LastSeenAt = o.LastSeenAt,
                    SignedInAt = sessions.TryGetValue(o.UserId, out var started) ? started : null,
                };
            })
            .ToList();
    }
}

public class UserSessionDto
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string Name { get; init; } = null!;

    public string Email { get; init; } = null!;

    public UserRole Role { get; init; }

    public string? IpAddress { get; init; }

    public string Client { get; init; } = null!;

    public DateTime StartedAt { get; init; }

    public DateTime LastSeenAt { get; init; }
}

/// <summary>
/// Recent sign-ins, newest first. A SuperAdmin sees every account's, including
/// other SuperAdmins; an Admin sees everybody except SuperAdmins.
/// </summary>
public record GetUserSessionsQuery : IRequest<List<UserSessionDto>>
{
    public Guid? UserId { get; init; }

    public int Days { get; init; } = 7;

    public int Limit { get; init; } = 200;
}

public class GetUserSessionsQueryHandler
    : IRequestHandler<GetUserSessionsQuery, List<UserSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetUserSessionsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<List<UserSessionDto>> Handle(
        GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var since = _clock.UtcNow.AddDays(-Math.Clamp(request.Days, 1, 365));
        var limit = Math.Clamp(request.Limit, 1, 500);
        var seesSuperAdmins = _currentUser.Role == UserRole.SuperAdmin;

        var query = _context.UserSessions
            .AsNoTracking()
            .Where(s => s.StartedAt >= since);

        if (request.UserId is { } userId)
        {
            query = query.Where(s => s.UserId == userId);
        }

        if (!seesSuperAdmins)
        {
            query = query.Where(s => s.User.Role != UserRole.SuperAdmin);
        }

        var rows = await query
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .Select(s => new
            {
                s.Id,
                s.UserId,
                s.User.Email,
                s.User.Role,
                FirstName = s.User.Employee != null ? s.User.Employee.FirstName : null,
                LastName = s.User.Employee != null ? s.User.Employee.LastName : null,
                s.IpAddress,
                s.UserAgent,
                s.StartedAt,
                s.LastSeenAt,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(s =>
            {
                var name = $"{s.FirstName} {s.LastName}".Trim();

                return new UserSessionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Name = name.Length > 0 ? name : s.Email,
                    Email = s.Email,
                    Role = s.Role,
                    IpAddress = s.IpAddress,
                    Client = PresenceClient.FromUserAgent(s.UserAgent),
                    StartedAt = s.StartedAt,
                    LastSeenAt = s.LastSeenAt,
                };
            })
            .ToList();
    }
}
