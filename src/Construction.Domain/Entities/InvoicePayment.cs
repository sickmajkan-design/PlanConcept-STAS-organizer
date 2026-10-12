using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Money a client paid against one invoice. An invoice can be paid in parts; what has been paid is the sum of
/// these, and the invoice is <see cref="InvoiceStatus.Paid"/> when that reaches its amount.
/// </summary>
public class InvoicePayment : BaseEntity, IAuditable
{
    public Guid InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = null!;

    /// <summary>Always more than zero, and never more than what was still open when it was recorded.</summary>
    public decimal Amount { get; set; }

    public DateOnly PaidOn { get; set; }

    public BookPaymentMethod? Method { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
