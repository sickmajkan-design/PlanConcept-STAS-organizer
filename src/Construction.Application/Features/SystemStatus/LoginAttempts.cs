using Construction.Application.Common.Interfaces;
using Construction.Application.Features.StorageCleanup;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SystemStatus;

public class LoginDayDto
{
    public DateOnly Day { get; init; }

    public int Count { get; init; }
}

public class LoginReasonDto
{
    public string Reason { get; init; } = string.Empty;

    public int Count { get; init; }
}

public class LoginIpDto
{
    public string Ip { get; init; } = string.Empty;

    public int Count { get; init; }

    /// <summary>How many different accounts this address tried: many is a guessing run, one is usually a person who forgot.</summary>
    public int Accounts { get; init; }

    public DateTime LastAtUtc { get; init; }
}

public class LoginAccountDto
{
    public string Email { get; init; } = string.Empty;

    public int Count { get; init; }
}

/// <summary>What fail2ban on the host has done to the SSH door (read from <c>bans.txt</c>, written by the host script).</summary>
public class SshBansDto
{
    public bool Known { get; init; }

    public int BannedNow { get; init; }

    public int BannedTotal { get; init; }

    public int FailedTotal { get; init; }

    public List<string> BannedIps { get; init; } = [];
}

public class LoginAttemptsDto
{
    public int Last24Hours { get; init; }

    public int Last7Days { get; init; }

    /// <summary>The last seven days, oldest first, a zero for a quiet day.</summary>
    public List<LoginDayDto> Days { get; init; } = [];

    public List<LoginReasonDto> Reasons { get; init; } = [];

    public List<LoginIpDto> TopIps { get; init; } = [];

    /// <summary>Only accounts that exist: a name typed by a stranger that matches nobody is counted under its reason, not listed.</summary>
    public List<LoginAccountDto> TopAccounts { get; init; } = [];

    public SshBansDto Ssh { get; init; } = new();
}

public static class SshBansParser
{
    public static SshBansDto Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SshBansDto();
        }

        var values = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => parts[0])
            .ToDictionary(g => g.Key, g => g.First()[1].Trim());

        int Number(string key) => values.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : 0;

        return new SshBansDto
        {
            Known = values.ContainsKey("banned_now"),
            BannedNow = Number("banned_now"),
            BannedTotal = Number("banned_total"),
            FailedTotal = Number("failed_total"),
            BannedIps = values.TryGetValue("ips", out var ips)
                ? ips.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(30).ToList()
                : [],
        };
    }
}

public record GetLoginAttemptsQuery : IRequest<LoginAttemptsDto>;

public class GetLoginAttemptsQueryHandler : IRequestHandler<GetLoginAttemptsQuery, LoginAttemptsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IDockerStatusReader _docker;

    public GetLoginAttemptsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock, IDockerStatusReader docker)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _docker = docker;
    }

    public async Task<LoginAttemptsDto> Handle(GetLoginAttemptsQuery request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        var now = _clock.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var from = today.AddDays(-6).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rows = await _context.FailedLogins.AsNoTracking()
            .Where(f => f.OccurredAt >= from)
            .Select(f => new { f.OccurredAt, f.Reason, f.IpAddress, f.Email, f.UserId })
            .ToListAsync(cancellationToken);

        var dayCounts = rows.GroupBy(r => DateOnly.FromDateTime(r.OccurredAt)).ToDictionary(g => g.Key, g => g.Count());

        return new LoginAttemptsDto
        {
            Last24Hours = rows.Count(r => r.OccurredAt >= now.AddHours(-24)),
            Last7Days = rows.Count,
            Days = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(i - 6))
                .Select(d => new LoginDayDto { Day = d, Count = dayCounts.GetValueOrDefault(d) })
                .ToList(),
            Reasons = rows.GroupBy(r => r.Reason)
                .Select(g => new LoginReasonDto { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToList(),
            TopIps = rows.Where(r => !string.IsNullOrEmpty(r.IpAddress))
                .GroupBy(r => r.IpAddress!)
                .Select(g => new LoginIpDto
                {
                    Ip = g.Key,
                    Count = g.Count(),
                    Accounts = g.Select(r => r.Email.ToLowerInvariant()).Distinct().Count(),
                    LastAtUtc = g.Max(r => r.OccurredAt),
                })
                .OrderByDescending(i => i.Count)
                .Take(10)
                .ToList(),
            TopAccounts = rows.Where(r => r.UserId != null)
                .GroupBy(r => r.Email.ToLowerInvariant())
                .Select(g => new LoginAccountDto { Email = g.Key, Count = g.Count() })
                .OrderByDescending(a => a.Count)
                .Take(10)
                .ToList(),
            Ssh = SshBansParser.Parse(_docker.ReadBans()),
        };
    }
}
