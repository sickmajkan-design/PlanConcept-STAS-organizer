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

    /// <summary>Every posting this employee currently holds — never just one.</summary>
    public IReadOnlyList<AssignmentBoardPostingDto> Postings { get; init; } =
        Array.Empty<AssignmentBoardPostingDto>();
}

/// <summary>One employee's posting to one site, dated.</summary>
public class AssignmentBoardPostingDto
{
    public Guid ProjectId { get; init; }

    public DateOnly StartDate { get; init; }

    /// <summary>Null while the posting is open-ended.</summary>
    public DateOnly? EndDate { get; init; }
}

public class AssignmentBoardProjectDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Status { get; init; } = null!;
}
