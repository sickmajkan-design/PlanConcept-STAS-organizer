using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.StorageCleanup;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SystemStatus;

/// <summary>What an error looks like once everything that could identify a person is left out.</summary>
public static class ServerErrorFingerprint
{
    public static (string Type, string Location) Describe(Exception exception)
    {
        var innermost = exception;

        while (innermost.InnerException is { } inner)
        {
            innermost = inner;
        }

        var type = innermost == exception ? exception.GetType().Name : $"{exception.GetType().Name} > {innermost.GetType().Name}";

        return (Cap(type, 200), Cap(Location(innermost) ?? Location(exception) ?? "-", 200));
    }

    public static string Key(string type, string location, string route) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{type}|{location}|{route}")));

    /// <summary>The first frame inside this application, with the async state machine's name turned back into the method's.</summary>
    private static string? Location(Exception exception)
    {
        foreach (var frame in new StackTrace(exception).GetFrames())
        {
            var method = frame.GetMethod();
            var declaring = method?.DeclaringType;

            if (method is null || declaring is null || declaring.Namespace?.StartsWith("Construction", StringComparison.Ordinal) != true)
            {
                continue;
            }

            var name = method.Name;
            var owner = declaring;

            // `<Handle>d__5.MoveNext` inside `SendFooHandler` is `SendFooHandler.Handle`.
            if (declaring.Name.StartsWith('<') && declaring.DeclaringType is { } outer)
            {
                var match = Regex.Match(declaring.Name, "^<(.+?)>");
                name = match.Success ? match.Groups[1].Value : name;
                owner = outer;
            }

            return $"{owner.Name}.{name}";
        }

        return null;
    }

    private static string Cap(string value, int length) => value.Length <= length ? value : value[..length];
}

/// <summary>
/// Collects unexpected errors in memory until the next flush, so that an error costs the request nothing and a database
/// that is itself the problem cannot make it worse. Counts, never messages.
/// </summary>
public sealed class ServerErrorBuffer
{
    public sealed class Entry
    {
        public string Key { get; init; } = string.Empty;

        public DateOnly Day { get; init; }

        public string ExceptionType { get; init; } = string.Empty;

        public string Location { get; init; } = string.Empty;

        public string Route { get; init; } = string.Empty;

        public string Version { get; init; } = string.Empty;

        public int Count { get; set; }

        public DateTime FirstSeenAt { get; set; }

        public DateTime LastSeenAt { get; set; }
    }

    /// <summary>More than this many different kinds in one minute is a flood, and the rest are dropped.</summary>
    private const int MaxKinds = 500;

    private readonly object _gate = new();
    private Dictionary<(string, DateOnly), Entry> _pending = [];

    /// <summary>Never throws: reporting an error must not become a second one.</summary>
    public void Record(Exception exception, string route, string version, DateTime nowUtc)
    {
        try
        {
            var (type, location) = ServerErrorFingerprint.Describe(exception);
            var routeText = route.Length <= 200 ? route : route[..200];
            var key = ServerErrorFingerprint.Key(type, location, routeText);
            var day = DateOnly.FromDateTime(nowUtc);

            lock (_gate)
            {
                if (_pending.TryGetValue((key, day), out var entry))
                {
                    entry.Count++;
                    entry.LastSeenAt = nowUtc;
                }
                else if (_pending.Count < MaxKinds)
                {
                    _pending[(key, day)] = new Entry
                    {
                        Key = key, Day = day, ExceptionType = type, Location = location, Route = routeText,
                        Version = version.Length <= 40 ? version : version[..40], Count = 1, FirstSeenAt = nowUtc, LastSeenAt = nowUtc,
                    };
                }
            }
        }
        catch (Exception)
        {
            // Deliberately nothing.
        }
    }

    public List<Entry> Drain()
    {
        lock (_gate)
        {
            var taken = _pending.Values.ToList();
            _pending = [];

            return taken;
        }
    }

    /// <summary>Puts back what could not be saved, adding to anything recorded meanwhile.</summary>
    public void Return(IEnumerable<Entry> entries)
    {
        lock (_gate)
        {
            foreach (var entry in entries)
            {
                if (_pending.TryGetValue((entry.Key, entry.Day), out var current))
                {
                    current.Count += entry.Count;
                    current.FirstSeenAt = entry.FirstSeenAt < current.FirstSeenAt ? entry.FirstSeenAt : current.FirstSeenAt;
                }
                else if (_pending.Count < MaxKinds)
                {
                    _pending[(entry.Key, entry.Day)] = entry;
                }
            }
        }
    }
}

/// <summary>Writes what the buffer collected into the database, and now and then removes what is more than 30 days old.</summary>
public record FlushServerErrorsCommand(bool Prune = false) : IRequest<int>;

public class FlushServerErrorsCommandHandler : IRequestHandler<FlushServerErrorsCommand, int>
{
    public const int KeepDays = 30;

    private readonly IApplicationDbContext _context;
    private readonly ServerErrorBuffer _buffer;
    private readonly IDateTimeProvider _clock;

    public FlushServerErrorsCommandHandler(IApplicationDbContext context, ServerErrorBuffer buffer, IDateTimeProvider clock)
    {
        _context = context;
        _buffer = buffer;
        _clock = clock;
    }

    public async Task<int> Handle(FlushServerErrorsCommand request, CancellationToken cancellationToken)
    {
        var entries = _buffer.Drain();

        try
        {
            if (entries.Count > 0)
            {
                var days = entries.Select(e => e.Day).Distinct().ToList();
                var existing = await _context.ServerErrors.Where(e => days.Contains(e.Day)).ToListAsync(cancellationToken);
                var byKey = existing.ToDictionary(e => (e.Key, e.Day));

                foreach (var entry in entries)
                {
                    if (byKey.TryGetValue((entry.Key, entry.Day), out var row))
                    {
                        row.Count += entry.Count;
                        row.LastSeenAt = entry.LastSeenAt > row.LastSeenAt ? entry.LastSeenAt : row.LastSeenAt;
                    }
                    else
                    {
                        _context.ServerErrors.Add(new ServerError
                        {
                            Id = Guid.NewGuid(), Key = entry.Key, Day = entry.Day, ExceptionType = entry.ExceptionType,
                            Location = entry.Location, Route = entry.Route, Count = entry.Count, Version = entry.Version,
                            FirstSeenAt = entry.FirstSeenAt, LastSeenAt = entry.LastSeenAt,
                        });
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            if (request.Prune)
            {
                var cutoff = DateOnly.FromDateTime(_clock.UtcNow).AddDays(-KeepDays);
                await _context.ServerErrors.Where(e => e.Day < cutoff).ExecuteDeleteAsync(cancellationToken);
            }
        }
        catch (Exception) when (entries.Count > 0)
        {
            _buffer.Return(entries);
            throw;
        }

        return entries.Count;
    }
}

public class ServerErrorDto
{
    public string ExceptionType { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;

    public int Today { get; init; }

    /// <summary>The last seven days including today.</summary>
    public int Total { get; init; }

    public DateTime LastSeenAtUtc { get; init; }

    /// <summary>The release the error was first seen in within the window.</summary>
    public string FirstVersion { get; init; } = string.Empty;
}

public record GetServerErrorsQuery : IRequest<List<ServerErrorDto>>;

public class GetServerErrorsQueryHandler : IRequestHandler<GetServerErrorsQuery, List<ServerErrorDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetServerErrorsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<List<ServerErrorDto>> Handle(GetServerErrorsQuery request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var from = today.AddDays(-6);

        var rows = await _context.ServerErrors.AsNoTracking().Where(e => e.Day >= from).ToListAsync(cancellationToken);

        return rows
            .GroupBy(e => e.Key)
            .Select(g =>
            {
                var first = g.OrderBy(e => e.Day).ThenBy(e => e.FirstSeenAt).First();

                return new ServerErrorDto
                {
                    ExceptionType = first.ExceptionType,
                    Location = first.Location,
                    Route = first.Route,
                    Today = g.Where(e => e.Day == today).Sum(e => e.Count),
                    Total = g.Sum(e => e.Count),
                    LastSeenAtUtc = g.Max(e => e.LastSeenAt),
                    FirstVersion = first.Version,
                };
            })
            .OrderByDescending(e => e.LastSeenAtUtc)
            .Take(50)
            .ToList();
    }
}
