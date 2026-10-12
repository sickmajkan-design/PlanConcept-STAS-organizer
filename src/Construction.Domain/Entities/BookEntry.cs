using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One line in the company's book of money — for money that has no other home.
/// </summary>
/// <remarks>
/// Fuel, vehicle and tool costs, general expenses, pay entries and invoices keep living in their
/// own tables and are shown in the book by a query that reads them; nothing is copied here, so a
/// figure has one source of truth. An entry here is for what the other tables cannot hold: a
/// bookkeeping fee, a fixed cost, a payment received that is not an invoice yet.
/// <para>
/// The amount is positive and the <see cref="Kind"/> says which way it goes, the same as
/// <see cref="Refund"/>. The state "open / paid" is derived from <see cref="PaidOn"/>.
/// </para>
/// </remarks>
public class BookEntry : BaseEntity, ISoftDeletable, IAuditable
{
    public BookEntryKind Kind { get; set; }

    public DateOnly OccurredOn { get; set; }

    /// <summary>Always more than zero, in the system's single currency.</summary>
    public decimal Amount { get; set; }

    public Guid CategoryId { get; set; }

    public BookCategory Category { get; set; } = null!;

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    /// <summary>Who was paid, or who paid — a supplier, a landlord, a client's name.</summary>
    public string? Counterparty { get; set; }

    public string? Description { get; set; }

    public BookPaymentMethod? Method { get; set; }

    /// <summary>When a cost or receivable is due. Null means no deadline.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>When it was actually paid. Null means still open.</summary>
    public DateOnly? PaidOn { get; set; }

    public BookEntrySource Source { get; set; } = BookEntrySource.Manual;

    /// <summary>The template or import row that produced it, so a second run cannot duplicate it.</summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Set by a client that may send the same entry twice, such as a phone that was offline.
    /// Unlike an idempotency key it does not expire, so a late retry is still recognised.
    /// </summary>
    public Guid? ClientRequestId { get; set; }

    /// <summary>The template that made this entry, with the period it was made for, so no period is made twice.</summary>
    public Guid? RecurringExpenseId { get; set; }

    public RecurringExpense? RecurringExpense { get; set; }

    public int? PeriodYear { get; set; }

    public int? PeriodMonth { get; set; }

    /// <summary>The business unit this is booked to. Null means it follows its project, if it has one.</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
