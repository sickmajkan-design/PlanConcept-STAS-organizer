namespace Construction.Application.Features.Assignments.Models;

/// <summary>
/// Everyone available to staff, every site worth staffing, and who is on
/// which — the whole picture a drag-and-drop assignment board needs in one
/// trip.
/// </summary>
public class AssignmentBoardDto
{
    public IReadOnlyList<AssignmentBoardEmployeeDto> Employees { get; init; } =
        Array.Empty<AssignmentBoardEmployeeDto>();

    public IReadOnlyList<AssignmentBoardProjectDto> Projects { get; init; } =
        Array.Empty<AssignmentBoardProjectDto>();
}

public class AssignmentBoardEmployeeDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = null!;

    public string EmployeeNumber { get; init; } = null!;

    public string Position { get; init; } = null!;

    /// <summary>Every project this employee is currently posted to — never just one.</summary>
    public IReadOnlyList<Guid> ProjectIds { get; init; } = Array.Empty<Guid>();
}

public class AssignmentBoardProjectDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Status { get; init; } = null!;
}
