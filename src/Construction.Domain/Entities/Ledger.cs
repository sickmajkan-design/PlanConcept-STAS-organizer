using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One month of the SuperAdmin's own free-form record-keeping — the in-app
/// equivalent of a monthly Excel tab. Columns, sections and rows are all
/// user-defined; nothing here is schema the rest of the app understands.
/// </summary>
public class Ledger : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    /// <summary>
    /// The business unit whose payroll this month is, when it is one unit's. Each unit that employs
    /// people runs its own, so a ledger of a unit is checked against who that unit employed in the month.
    /// Null means a company-wide month, as before.
    /// </summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public string? Note { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    /// <summary>
    /// When the month was closed. A locked month refuses changes to its cells, columns, sections and rows, so
    /// what was reported to the accountant cannot move by accident.
    /// </summary>
    public DateTime? LockedAt { get; set; }

    public Guid? LockedByUserId { get; set; }

    public User? LockedByUser { get; set; }

    /// <summary>
    /// The month's figures as they were when it was closed (see <c>LedgerMonthResult</c>), so a closed month is
    /// read from here and does not change when prices, rates or the sources it was worked out from do.
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>Why the month was last reopened. The audit trail keeps the earlier ones.</summary>
    public string? UnlockNote { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<LedgerColumn> Columns { get; set; } = new List<LedgerColumn>();

    public ICollection<LedgerSection> Sections { get; set; } = new List<LedgerSection>();

    public ICollection<LedgerSummaryBox> SummaryBoxes { get; set; } = new List<LedgerSummaryBox>();
}
