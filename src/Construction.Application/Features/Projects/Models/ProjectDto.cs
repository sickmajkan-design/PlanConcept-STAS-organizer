using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Projects.Models;

public class ProjectDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string? Client { get; init; }

    public string? Address { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string Status { get; init; } = null!;

    public int EmployeeCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Project"/> becomes an <see cref="ProjectDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class ProjectMapping
{
    public static readonly Expression<Func<Project, ProjectDto>> Projection = project =>
        new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Client = project.Client,
            Address = project.Address,
            Latitude = project.Latitude,
            Longitude = project.Longitude,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status.ToString(),
            // Open-ended assignments only — see the note in
            // EmployeeDetailMapping.Projection (Features/Employees/Models).
            EmployeeCount = project.EmployeeAssignments.Count(a => a.EndDate == null),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
        };

    private static readonly Func<Project, ProjectDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static ProjectDto ToDto(Project project) => Compiled(project);
}
