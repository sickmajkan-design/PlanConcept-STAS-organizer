using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Construction.Application.Features.SystemStatus;
using Construction.Infrastructure.Persistence;
using Construction.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Construction.Infrastructure.Monitoring;

/// <summary>
/// What the running process and the machine can tell about themselves, for the server status page.
/// </summary>
/// <remarks>
/// In the container the host's own figures are readable from <c>/proc</c> (uptime, load, memory), which is why this does not
/// need access to Docker, and the API deliberately has none. A figure that cannot be read is null and the page shows a dash;
/// nothing here may fail a request. Containers, images and the backup job are on the host and not visible from here: the
/// disk reading and the nightly clean-up log cover those.
/// </remarks>
public sealed class ServerProbe : IServerProbe
{
    private readonly ApplicationDbContext _db;
    private readonly FileStorageSettings _storage;

    public ServerProbe(ApplicationDbContext db, IOptions<FileStorageSettings> storage)
    {
        _db = db;
        _storage = storage.Value;
    }

    public string FileStorageKind => _storage.UsesObjectStorage ? "objectStorage" : "disk";

    public HostStatusDto ReadHost()
    {
        var uptime = ProcFiles.ParseUptime(ReadOrNull("/proc/uptime"));
        var load = ProcFiles.ParseLoadAverage(ReadOrNull("/proc/loadavg"));
        var memory = ProcFiles.ParseMemInfo(ReadOrNull("/proc/meminfo"));

        return new HostStatusDto
        {
            Known = uptime is not null || load is not null || memory is not null,
            Os = RuntimeInformation.OSDescription,
            // Windows keeps its own uptime; the rest stays unknown there.
            UptimeSeconds = uptime ?? (OperatingSystem.IsWindows() ? Environment.TickCount64 / 1000 : null),
            Cores = Environment.ProcessorCount,
            Load1 = load?.One,
            Load5 = load?.Five,
            Load15 = load?.Fifteen,
            MemoryTotalBytes = memory?.TotalBytes,
            MemoryAvailableBytes = memory?.AvailableBytes,
            SwapTotalBytes = memory?.SwapTotalBytes,
            SwapFreeBytes = memory?.SwapFreeBytes,
        };
    }

    public ApplicationStatusDto ReadApplication()
    {
        using var process = Process.GetCurrentProcess();
        var started = process.StartTime.ToUniversalTime();

        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";

        return new ApplicationStatusDto
        {
            StartedAtUtc = started,
            UptimeSeconds = (long)(DateTime.UtcNow - started).TotalSeconds,
            // The build adds the commit after a "+"; the first part is the version a person recognises.
            Version = version.Split('+')[0],
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Runtime = RuntimeInformation.FrameworkDescription,
            ProcessMemoryBytes = process.WorkingSet64,
            ManagedHeapBytes = GC.GetTotalMemory(forceFullCollection: false),
            Threads = process.Threads.Count,
        };
    }

    public async Task<DatabaseStatusDto> ReadDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var database = _db.Database;

            var version = await database
                .SqlQuery<string>($"SELECT split_part(version(), ' on ', 1) AS \"Value\"")
                .FirstAsync(cancellationToken);

            var size = await database
                .SqlQuery<long>($"SELECT pg_database_size(current_database()) AS \"Value\"")
                .FirstAsync(cancellationToken);

            var connections = await database
                .SqlQuery<ConnectionRow>(
                    $"SELECT count(*)::int AS \"Total\", (count(*) FILTER (WHERE state = 'active'))::int AS \"Active\" FROM pg_stat_activity WHERE datname = current_database()")
                .FirstAsync(cancellationToken);

            var maxConnections = await database
                .SqlQuery<int>($"SELECT setting::int AS \"Value\" FROM pg_settings WHERE name = 'max_connections'")
                .FirstAsync(cancellationToken);

            var hits = await database
                .SqlQuery<HitRow>($"SELECT blks_hit AS \"Hit\", blks_read AS \"Read\" FROM pg_stat_database WHERE datname = current_database()")
                .FirstOrDefaultAsync(cancellationToken);

            var tables = await database
                .SqlQuery<TableRow>(
                    $"SELECT c.relname AS \"Name\", pg_total_relation_size(c.oid) AS \"Bytes\" FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relkind = 'r' ORDER BY 2 DESC LIMIT 5")
                .ToListAsync(cancellationToken);

            return new DatabaseStatusDto
            {
                Reachable = true,
                Version = version,
                SizeBytes = size,
                Connections = connections.Total,
                ActiveConnections = connections.Active,
                MaxConnections = maxConnections,
                CacheHitPercent = hits is { } h && h.Hit + h.Read > 0
                    ? Math.Round(h.Hit * 100.0 / (h.Hit + h.Read), 1)
                    : null,
                LargestTables = tables.Select(t => new TableSizeDto { Name = t.Name, Bytes = t.Bytes }).ToList(),
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // An unreachable database is the answer, not a failure of the page that reports it.
            return new DatabaseStatusDto { Reachable = false };
        }
    }

    private static string? ReadOrNull(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record ConnectionRow(int Total, int Active);

    private sealed record HitRow(long Hit, long Read);

    private sealed record TableRow(string Name, long Bytes);
}

/// <summary>Reads the three <c>/proc</c> files, as text, so they can be tested without a Linux machine.</summary>
public static class ProcFiles
{
    public readonly record struct LoadAverage(double One, double Five, double Fifteen);

    public readonly record struct MemoryInfo(long TotalBytes, long AvailableBytes, long SwapTotalBytes, long SwapFreeBytes);

    /// <summary><c>12345.67 54321.00</c>: seconds since boot, then idle time.</summary>
    public static long? ParseUptime(string? text)
    {
        var first = text?.Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? (long)seconds
            : null;
    }

    /// <summary><c>0.15 0.10 0.05 1/234 5678</c>: the load of the last 1, 5 and 15 minutes.</summary>
    public static LoadAverage? ParseLoadAverage(string? text)
    {
        var parts = text?.Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (parts is { Length: >= 3 }
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var one)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var five)
            && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var fifteen))
        {
            return new LoadAverage(one, five, fifteen);
        }

        return null;
    }

    /// <summary><c>MemTotal: 3865476 kB</c> lines. "Available" is what can be given out without swapping, the figure that matters.</summary>
    public static MemoryInfo? ParseMemInfo(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var kilobytes = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = line.IndexOf(':');

            if (colon < 0)
            {
                continue;
            }

            var number = line[(colon + 1)..].Replace("kB", string.Empty, StringComparison.Ordinal).Trim();

            if (long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                kilobytes[line[..colon].Trim()] = value;
            }
        }

        if (!kilobytes.TryGetValue("MemTotal", out var total) || !kilobytes.TryGetValue("MemAvailable", out var available))
        {
            return null;
        }

        kilobytes.TryGetValue("SwapTotal", out var swapTotal);
        kilobytes.TryGetValue("SwapFree", out var swapFree);

        return new MemoryInfo(total * 1024, available * 1024, swapTotal * 1024, swapFree * 1024);
    }
}
