using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.TimeEntries.Models;

/// <summary>
/// Great-circle distance helpers, used only in memory (never inside an EF
/// Core query expression) so their trig never has to survive translation
/// into SQL.
/// </summary>
public static class GeoDistance
{
    private const double EarthRadiusMeters = 6_371_000;

    /// <summary>Haversine distance between two points, in meters.</summary>
    public static double Meters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}

public class TimeEntryDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? EndedAt { get; init; }

    public int BreakMinutes { get; init; }

    /// <summary>
    /// Paid minutes, or null while the shift is still running.
    ///
    /// Computed here rather than projected, so the arithmetic never has to
    /// survive translation into SQL. Sorting and aggregation that do need it
    /// in the database spell the expression out in the query instead.
    /// </summary>
    public int? WorkedMinutes => EndedAt is null
        ? null
        : (int)(EndedAt.Value - StartedAt).TotalMinutes - BreakMinutes;

    public WorkType WorkType { get; init; }

    public TimeEntryStatus Status { get; init; }

    public string? Note { get; init; }

    public double? StartLatitude { get; init; }

    public double? StartLongitude { get; init; }

    public double? EndLatitude { get; init; }

    public double? EndLongitude { get; init; }

    public double? ProjectLatitude { get; init; }

    public double? ProjectLongitude { get; init; }

    public TimeOnly? ProjectShiftStartTime { get; init; }

    /// <summary>Maximum distance from the project's coordinates still counted as "on site".</summary>
    private const double LocationToleranceMeters = 100;

    /// <summary>How far from the project's expected shift start still counts as "on time".</summary>
    private static readonly TimeSpan TimeTolerance = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Whether the clock-in happened within <see cref="LocationToleranceMeters"/>
    /// of the project's coordinates. Null when either the entry or the
    /// project has no coordinates to compare.
    ///
    /// Computed here rather than projected, for the same reason as
    /// <see cref="WorkedMinutes"/>: the Haversine trig should never have to
    /// survive translation into SQL.
    /// </summary>
    public bool? LocationCorrect => StartLatitude is null || StartLongitude is null
        || ProjectLatitude is null || ProjectLongitude is null
        ? null
        : GeoDistance.Meters(StartLatitude.Value, StartLongitude.Value, ProjectLatitude.Value, ProjectLongitude.Value)
            <= LocationToleranceMeters;

    /// <summary>
    /// Whether the clock-in happened within <see cref="TimeTolerance"/> of
    /// the project's expected shift start. Null when the project has no
    /// shift start time set.
    /// </summary>
    public bool? TimeCorrect => ProjectShiftStartTime is null
        ? null
        : Diff(TimeOnly.FromDateTime(StartedAt), ProjectShiftStartTime.Value) <= TimeTolerance;

    private static TimeSpan Diff(TimeOnly a, TimeOnly b)
    {
        var diff = a.ToTimeSpan() - b.ToTimeSpan();
        var abs = diff.Duration();
        // Wrap-around, so 23:55 vs 00:05 reads as 10 minutes apart, not ~24h.
        return abs > TimeSpan.FromHours(12) ? TimeSpan.FromHours(24) - abs : abs;
    }

    public string? ReviewedByName { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public string? ReviewNote { get; init; }

    public bool AutoClosed { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="TimeEntry"/> becomes an <see cref="TimeEntryDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class TimeEntryMapping
{
    public static readonly Expression<Func<TimeEntry, TimeEntryDto>> Projection = entry =>
        new TimeEntryDto
        {
            Id = entry.Id,
            EmployeeId = entry.EmployeeId,
            EmployeeName = entry.Employee.FirstName + " " + entry.Employee.LastName,
            ProjectId = entry.ProjectId,
            ProjectName = entry.Project != null ? entry.Project.Name : null,
            StartedAt = entry.StartedAt,
            EndedAt = entry.EndedAt,
            BreakMinutes = entry.BreakMinutes,
            WorkType = entry.WorkType,
            Status = entry.Status,
            Note = entry.Note,
            StartLatitude = entry.StartLatitude,
            StartLongitude = entry.StartLongitude,
            EndLatitude = entry.EndLatitude,
            EndLongitude = entry.EndLongitude,
            ProjectLatitude = entry.Project != null ? entry.Project.Latitude : null,
            ProjectLongitude = entry.Project != null ? entry.Project.Longitude : null,
            ProjectShiftStartTime = entry.Project != null ? entry.Project.ShiftStartTime : null,
            ReviewedByName = entry.ReviewedByUser != null ? entry.ReviewedByUser.Email : null,
            ReviewedAt = entry.ReviewedAt,
            ReviewNote = entry.ReviewNote,
            AutoClosed = entry.AutoClosed,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
        };

    private static readonly Func<TimeEntry, TimeEntryDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static TimeEntryDto ToDto(TimeEntry entry) => Compiled(entry);
}
