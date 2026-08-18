using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Projects.Models;

public class ProjectDetailDto : ProjectDto
{
    public IReadOnlyCollection<ProjectEmployeeDto> Employees { get; init; } =
        Array.Empty<ProjectEmployeeDto>();

    /// <summary>Crew whose posting here has ended, most recently closed first.</summary>
    public IReadOnlyCollection<ProjectEmployeeDto> PastEmployees { get; init; } =
        Array.Empty<ProjectEmployeeDto>();
}

public class ProjectEmployeeDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeNumber { get; init; } = null!;

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;

    public string Status { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    /// <summary>Null while the posting is still open.</summary>
    public DateOnly? EndDate { get; init; }

    public DateTime AssignedAt { get; init; }
}

/// <summary>
/// How a <see cref="Project"/> becomes an <see cref="ProjectDetailDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class ProjectDetailMapping
{
    public static readonly Expression<Func<Project, ProjectDetailDto>> Projection = project =>
        new ProjectDetailDto
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
            ContractValue = project.ContractValue,
            // Open-ended assignments only — see the matching note in
            // EmployeeDetailMapping.Projection. Without this, a project's
            // roster and count never shrank: everyone ever taken off the
            // project stayed listed, and "remove from project" looked like
            // it silently did nothing from this side too.
            EmployeeCount = project.EmployeeAssignments.Count(a => a.EndDate == null),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Employees = project.EmployeeAssignments
                .Where(assignment => assignment.EndDate == null)
                .Select(assignment => new ProjectEmployeeDto
                {
                    EmployeeId = assignment.EmployeeId,
                    EmployeeNumber = assignment.Employee.EmployeeNumber,
                    FullName = assignment.Employee.FirstName + " " + assignment.Employee.LastName,
                    Position = assignment.Employee.Position,
                    Status = assignment.Employee.Status.ToString(),
                    StartDate = assignment.StartDate,
                    EndDate = assignment.EndDate,
                    AssignedAt = assignment.AssignedAt,
                })
                .ToList(),
            PastEmployees = project.EmployeeAssignments
                .Where(assignment => assignment.EndDate != null)
                .OrderByDescending(assignment => assignment.EndDate)
                .Select(assignment => new ProjectEmployeeDto
                {
                    EmployeeId = assignment.EmployeeId,
                    EmployeeNumber = assignment.Employee.EmployeeNumber,
                    FullName = assignment.Employee.FirstName + " " + assignment.Employee.LastName,
                    Position = assignment.Employee.Position,
                    Status = assignment.Employee.Status.ToString(),
                    StartDate = assignment.StartDate,
                    EndDate = assignment.EndDate,
                    AssignedAt = assignment.AssignedAt,
                })
                .ToList(),
        };

    private static readonly Func<Project, ProjectDetailDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static ProjectDetailDto ToDto(Project project) => Compiled(project);
}
