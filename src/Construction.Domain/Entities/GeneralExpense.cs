using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A cost that doesn't belong to any vehicle, tool or material — bookkeeping
/// fees, damage, a customer complaint that cost something to resolve, a
/// worker-related cost outside payroll, or anything else that just needs a
/// category and an amount.
/// </summary>
/// <remarks>
/// Both <see cref="ProjectId"/> and <see cref="EmployeeId"/> are optional and
/// independent — a bookkeeping invoice belongs to neither, a damage claim
/// might belong to a site, a worker-related cost might belong to a person.
/// When <see cref="ProjectId"/> is set it folds into that project's cost
/// report the same way rental cost folds into a vehicle's.
/// </remarks>
public class GeneralExpense : BaseEntity, IAuditable
{
    public GeneralExpenseCategory Category { get; set; }

    /// <summary>What it cost, in the system's single currency.</summary>
    public decimal Amount { get; set; }

    public DateOnly OccurredOn { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    /// <summary>Who was paid, or who's responsible — a landlord, an accountant, a customer.</summary>
    public string? Supplier { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
