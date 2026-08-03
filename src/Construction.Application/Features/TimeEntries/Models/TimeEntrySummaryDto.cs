namespace Construction.Application.Features.TimeEntries.Models;

/// <summary>Hours per employee over a period.</summary>
public class TimeEntrySummaryDto
{
    public DateTime From { get; init; }

    public DateTime To { get; init; }

    public IReadOnlyCollection<TimeEntrySummaryRowDto> Rows { get; init; } =
        Array.Empty<TimeEntrySummaryRowDto>();

    /// <summary>Everything recorded in the period, whatever its review state.</summary>
    public int TotalMinutes => Rows.Sum(r => r.TotalMinutes);

    /// <summary>The part that is signed off, and so the part safe to pay against.</summary>
    public int ApprovedMinutes => Rows.Sum(r => r.ApprovedMinutes);

    /// <summary>How much of the period is still waiting on a reviewer.</summary>
    public int PendingCount => Rows.Sum(r => r.PendingCount);
}

public class TimeEntrySummaryRowDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public int EntryCount { get; init; }

    public int TotalMinutes { get; init; }

    public int ApprovedMinutes { get; init; }

    public int PendingCount { get; init; }
}
