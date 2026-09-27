using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// An invoice the firm issued to a client for the work on a site. The program records it; it
/// does not issue it.
/// </summary>
/// <remarks>
/// For a site billed by a fixed sum or by measured work, the billing in the payroll is the sum of
/// these invoices for the month they are attributed to. The amount is split among one or more
/// companies of the client (<see cref="Shares"/>); the parts always add up to the whole.
/// </remarks>
public class Invoice : BaseEntity, IAuditable
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    /// <summary>The number on the invoice as the firm's accounting issued it. Unique.</summary>
    public string Number { get; set; } = null!;

    public DateOnly IssueDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// The whole invoice in the system's single currency. Negative for a credit note, never zero.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>The payroll month this invoice is counted in; it need not be the month it was issued.</summary>
    public int PayrollYear { get; set; }

    public int PayrollMonth { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

    /// <summary>Why it was cancelled.</summary>
    public string? CancelReason { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<InvoiceShare> Shares { get; set; } = new List<InvoiceShare>();
}

/// <summary>One company's part of an invoice.</summary>
public class InvoiceShare : BaseEntity
{
    public Guid InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = null!;

    /// <summary>The company invoiced for this part. Null means the client itself, when it has no companies.</summary>
    public Guid? CustomerCompanyId { get; set; }

    public CustomerCompany? CustomerCompany { get; set; }

    public decimal Amount { get; set; }
}
