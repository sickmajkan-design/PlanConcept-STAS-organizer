using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.TimeEntries.Models;

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

    public string? ReviewedByName { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public string? ReviewNote { get; init; }

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
            ReviewedByName = entry.ReviewedByUser != null ? entry.ReviewedByUser.Email : null,
            ReviewedAt = entry.ReviewedAt,
            ReviewNote = entry.ReviewNote,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
        };

    private static readonly Func<TimeEntry, TimeEntryDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static TimeEntryDto ToDto(TimeEntry entry) => Compiled(entry);
}
