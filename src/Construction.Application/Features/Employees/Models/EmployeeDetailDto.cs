using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Employees.Models;

public class EmployeeDetailDto : EmployeeDto
{
    public bool HasUserAccount { get; init; }

    public IReadOnlyCollection<EmployeeProjectAssignmentDto> Projects { get; init; } =
        Array.Empty<EmployeeProjectAssignmentDto>();

    /// <summary>Postings that have ended, most recently closed first.</summary>
    public IReadOnlyCollection<EmployeeProjectAssignmentDto> PastProjects { get; init; } =
        Array.Empty<EmployeeProjectAssignmentDto>();
}

public class EmployeeProjectAssignmentDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public string ProjectStatus { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    /// <summary>Null while the posting is still open.</summary>
    public DateOnly? EndDate { get; init; }

    public DateTime AssignedAt { get; init; }

    // Filled in by the handler after the projection runs, from finance
    // entries — a second query the single-expression projection above cannot
    // itself join in and stay translatable. Settable rather than init for
    // exactly that reason: these are the only three fields on this DTO a
    // caller other than the mapping ever assigns.

    /// <summary>Hours paid for on this posting, from hourly finance entries.</summary>
    public decimal WorkedHours { get; set; }

    /// <summary>Days paid for on this posting, from daily finance entries.</summary>
    public int WorkedDays { get; set; }

    /// <summary>
    /// What this posting has been paid so far, across every kind of finance
    /// entry recorded against it. Null rather than zero for a role the API
    /// does not show pay to — a foreman sees no figure here, not a wrong one.
    /// </summary>
    public decimal? TotalPay { get; set; }
}

/// <summary>
/// The single-employee view: everything the list carries, plus the assignments.
/// </summary>
/// <remarks>
/// The base columns are repeated rather than inherited from
/// <see cref="EmployeeMapping.Projection"/>. EF composes a projection from one
/// expression tree it can read end to end; a call to another expression is a
/// method call to it, and it will not translate one. The duplication is the
/// price of the query staying a single round trip, and
/// <c>EmployeeProjectionTests</c> holds the two shapes to the same values.
/// </remarks>
public static class EmployeeDetailMapping
{
    public static readonly Expression<Func<Employee, EmployeeDetailDto>> Projection = employee =>
        new EmployeeDetailDto
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = employee.FirstName + " " + employee.LastName,
            Phone = employee.Phone,
            Email = employee.Email,
            Address = employee.Address,
            DateOfBirth = employee.DateOfBirth,
            EmploymentDate = employee.EmploymentDate,
            Position = employee.Position,
            Status = employee.Status.ToString(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            HasUserAccount = employee.User != null,
            // Open-ended postings only — deliberately EndDate == null, not
            // "EndDate is today or later". RemoveEmployeeFromProjectCommand
            // doesn't delete an already-started assignment; it closes it with
            // EndDate = today so the timesheets it sits next to still agree
            // with the schedule. A `>= today` filter here (the same one that
            // command uses to *find* the posting to close) would count that
            // still-current-through-today record, so the project an admin
            // just removed someone from kept showing up here for the rest of
            // the day — "remove from project" looked like it silently did
            // nothing. This DTO answers "who is on this project right now",
            // which any EndDate — even today's — already answers "no" to.
            Projects = employee.ProjectAssignments
                .Where(assignment => assignment.EndDate == null)
                .Select(assignment => new EmployeeProjectAssignmentDto
                {
                    ProjectId = assignment.ProjectId,
                    ProjectName = assignment.Project.Name,
                    ProjectStatus = assignment.Project.Status.ToString(),
                    StartDate = assignment.StartDate,
                    EndDate = assignment.EndDate,
                    AssignedAt = assignment.AssignedAt,
                })
                .ToList(),
            // The other half of the same collection: everything the filter
            // above leaves out. Kept on the same DTO rather than a separate
            // endpoint, since a screen showing where someone works now is the
            // same screen anyone asks "and before that?" on.
            PastProjects = employee.ProjectAssignments
                .Where(assignment => assignment.EndDate != null)
                .OrderByDescending(assignment => assignment.EndDate)
                .Select(assignment => new EmployeeProjectAssignmentDto
                {
                    ProjectId = assignment.ProjectId,
                    ProjectName = assignment.Project.Name,
                    ProjectStatus = assignment.Project.Status.ToString(),
                    StartDate = assignment.StartDate,
                    EndDate = assignment.EndDate,
                    AssignedAt = assignment.AssignedAt,
                })
                .ToList(),
        };

    private static readonly Func<Employee, EmployeeDetailDto> Compiled = Projection.Compile();

    public static EmployeeDetailDto ToDto(Employee employee) => Compiled(employee);
}
