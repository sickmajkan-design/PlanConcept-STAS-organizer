using AutoMapper;
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

public class TimeEntryDtoMappingProfile : Profile
{
    public TimeEntryDtoMappingProfile()
    {
        CreateMap<TimeEntry, TimeEntryDto>()
            .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s =>
                s.Employee.FirstName + " " + s.Employee.LastName))
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s =>
                s.Project != null ? s.Project.Name : null))
            .ForMember(d => d.ReviewedByName, opt => opt.MapFrom(s =>
                s.ReviewedByUser != null ? s.ReviewedByUser.Email : null));
    }
}
