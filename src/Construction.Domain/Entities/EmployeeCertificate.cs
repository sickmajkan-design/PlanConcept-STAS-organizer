using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Something a worker is qualified or licensed for, and until when: work at height, a forklift licence,
/// a welding certificate.
/// </summary>
/// <remarks>
/// A name and an end date, nothing more. The scan of the paper itself is an attachment on the employee;
/// this is what the schedule can reason about. Two names are the same certificate when they match
/// ignoring case and surrounding spaces.
/// </remarks>
public class EmployeeCertificate : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>The last day it is valid. Null for one that does not expire.</summary>
    public DateOnly? ValidUntil { get; set; }

    public string? Note { get; set; }
}
