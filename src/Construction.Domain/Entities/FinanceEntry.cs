using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A payroll line the office entered by hand: what an employee is owed for a
/// stretch of work, however it was priced.
/// </summary>
/// <remarks>
/// Deliberately separate from <see cref="EmployeeRate"/> and the labour cost
/// that a project's cost report derives from clocked hours. That figure is
/// what a project cost; this one is what the office actually decided to pay —
/// hours get rounded, a day is sometimes paid flat regardless of the clock,
/// and a figure occasionally needs a correction the timesheet should not be
/// rewritten to match. Kept manual on purpose, mirroring how the office
/// already does it.
/// </remarks>
public class FinanceEntry : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public FinanceEntryKind Kind { get; set; }

    /// <summary>What is owed, in the system's single currency.</summary>
    public decimal Amount { get; set; }

    public DateOnly OccurredOn { get; set; }

    /// <summary>The site the pay is charged against, when there is one.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>Hours paid for. Only ever set for <see cref="FinanceEntryKind.WorkerPaymentHourly"/>.</summary>
    public decimal? HoursWorked { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
