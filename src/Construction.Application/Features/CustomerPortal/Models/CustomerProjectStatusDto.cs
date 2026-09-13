using Construction.Domain.Enums;

namespace Construction.Application.Features.CustomerPortal.Models;

/// <summary>
/// What a customer is allowed to see about their own project: progress and
/// timeline, nothing financial. Deliberately its own DTO rather than a
/// narrowed reuse of the realization/cost models — those carry contract
/// value and revenue, and the only way to guarantee a customer's screen
/// never grows a price on it later is for the shape itself to have no field
/// to put one in.
/// </summary>
public class CustomerProjectStatusDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public ProjectStatus Status { get; init; }

    public string? Address { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>
    /// Share of the project's work items marked done, 0-100. Null when the
    /// project has no work items to measure against — not zero, which would
    /// read as "nothing done" rather than "nothing tracked yet".
    /// </summary>
    public int? PercentComplete { get; init; }
}
