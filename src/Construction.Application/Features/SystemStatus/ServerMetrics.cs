using System.Globalization;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.StorageCleanup;
using MediatR;

namespace Construction.Application.Features.SystemStatus;

public class MetricPointDto
{
    /// <summary>The start of the hour the averages cover.</summary>
    public DateTime AtUtc { get; init; }

    /// <summary>One-minute load average per core, as a percentage (100 means every core busy; it can go higher).</summary>
    public double LoadPercent { get; init; }

    public double MemoryPercent { get; init; }

    public double DiskPercent { get; init; }
}

public class ServerMetricsDto
{
    /// <summary>False until the host script has written anything.</summary>
    public bool Known { get; init; }

    /// <summary>Hourly averages for the last seven days, oldest first.</summary>
    public List<MetricPointDto> Points { get; init; } = [];

    /// <summary>How fast the disk fills, from the trend of the points. Null when it is not growing or there is under a day of data.</summary>
    public long? DiskGrowthBytesPerDay { get; init; }

    /// <summary>At that speed, how many days until the disk is full. Null with the growth, or when it is over a year.</summary>
    public int? DiskDaysLeft { get; init; }
}

/// <summary>Turns the host script's <c>time|load1|cores|memory%|disk%</c> lines (one per five minutes) into hourly points.</summary>
public static class ServerMetricsParser
{
    public static readonly TimeSpan Window = TimeSpan.FromDays(7);

    public static List<MetricPointDto> Parse(string? log, DateTime nowUtc)
    {
        var from = nowUtc - Window;
        var samples = new List<(DateTime At, double Load, double Memory, double Disk)>();

        foreach (var line in (log ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|');

            if (parts.Length >= 5
                && DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var at)
                && at >= from
                && at <= nowUtc.AddMinutes(5)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var load)
                && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var cores) && cores > 0
                && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var memory)
                && double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var disk))
            {
                samples.Add((at, load / cores * 100, memory, disk));
            }
        }

        return samples
            .GroupBy(s => new DateTime(s.At.Year, s.At.Month, s.At.Day, s.At.Hour, 0, 0, DateTimeKind.Utc))
            .OrderBy(g => g.Key)
            .Select(g => new MetricPointDto
            {
                AtUtc = g.Key,
                LoadPercent = Math.Round(g.Average(s => s.Load), 1),
                MemoryPercent = Math.Round(g.Average(s => s.Memory), 1),
                DiskPercent = Math.Round(g.Average(s => s.Disk), 1),
            })
            .ToList();
    }

    /// <summary>
    /// The slope of the disk percentage per day, by least squares over the points. Null when there is under a day of data,
    /// which is too little to tell a trend from the nightly clean-up.
    /// </summary>
    public static double? DiskPercentPerDay(IReadOnlyList<MetricPointDto> points)
    {
        if (points.Count < 2 || (points[^1].AtUtc - points[0].AtUtc) < TimeSpan.FromDays(1))
        {
            return null;
        }

        var t0 = points[0].AtUtc;
        var xs = points.Select(p => (p.AtUtc - t0).TotalDays).ToList();
        var meanX = xs.Average();
        var meanY = points.Average(p => p.DiskPercent);
        var denominator = xs.Sum(x => (x - meanX) * (x - meanX));

        return denominator == 0 ? null : xs.Zip(points, (x, p) => (x - meanX) * (p.DiskPercent - meanY)).Sum() / denominator;
    }
}

public record GetServerMetricsQuery : IRequest<ServerMetricsDto>;

public class GetServerMetricsQueryHandler : IRequestHandler<GetServerMetricsQuery, ServerMetricsDto>
{
    private readonly IDockerStatusReader _reader;
    private readonly IDiskSpace _disk;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetServerMetricsQueryHandler(IDockerStatusReader reader, IDiskSpace disk, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _reader = reader;
        _disk = disk;
        _currentUser = currentUser;
        _clock = clock;
    }

    public Task<ServerMetricsDto> Handle(GetServerMetricsQuery request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        var log = _reader.ReadMetricsLog();

        if (log is null)
        {
            return Task.FromResult(new ServerMetricsDto());
        }

        var points = ServerMetricsParser.Parse(log, _clock.UtcNow);
        long? growth = null;
        int? daysLeft = null;

        if (_disk.Read() is { } reading && ServerMetricsParser.DiskPercentPerDay(points) is > 0.05 and var slope)
        {
            growth = (long)(slope / 100 * reading.TotalBytes);
            var days = reading.FreeBytes / (double)growth.Value;
            daysLeft = days < 365 ? (int)days : null;
        }

        return Task.FromResult(new ServerMetricsDto { Known = true, Points = points, DiskGrowthBytesPerDay = growth, DiskDaysLeft = daysLeft });
    }
}
