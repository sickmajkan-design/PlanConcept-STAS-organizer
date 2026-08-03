using Construction.Domain.Enums;

namespace Construction.Application.Features.Absences.Models;

/// <summary>Who is where, and who is away, over a period.</summary>
public class ScheduleDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public IReadOnlyCollection<ScheduleRowDto> Rows { get; init; } =
        Array.Empty<ScheduleRowDto>();
}

/// <summary>One employee's line on the board.</summary>
public class ScheduleRowDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public string Position { get; init; } = null!;

    public IReadOnlyCollection<ScheduleAssignmentDto> Assignments { get; init; } =
        Array.Empty<ScheduleAssignmentDto>();

    public IReadOnlyCollection<ScheduleAbsenceDto> Absences { get; init; } =
        Array.Empty<ScheduleAbsenceDto>();
}

/// <summary>
/// A posting, clipped to the window being shown.
/// </summary>
/// <remarks>
/// Clipped so the client can render a bar without re-deriving where it starts:
/// an open-ended posting that began last year has no visible start inside this
/// week, and sending the real dates would make every client repeat the same
/// arithmetic.
/// </remarks>
public class ScheduleAssignmentDto
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>True when the posting runs on past the end of the window.</summary>
    public bool ContinuesAfter { get; init; }
}

public class ScheduleAbsenceDto
{
    public Guid Id { get; init; }

    public AbsenceType Type { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }
}
