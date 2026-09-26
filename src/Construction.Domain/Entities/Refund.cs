using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Money a person spent for the firm out of their own pocket, with the receipt and the
/// reason. Once approved it is paid back through the payroll of a month, which is what
/// <see cref="PayrollYear"/> and <see cref="PayrollMonth"/> say.
/// </summary>
public class Refund : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public User RequestedByUser { get; set; } = null!;

    /// <summary>The site the expense was for, when there is one.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public decimal Amount { get; set; }

    /// <summary>ISO 4217 code, as written on the receipt. Not converted anywhere.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>The day the money was spent.</summary>
    public DateOnly ExpenseDate { get; set; }

    /// <summary>Why the firm should pay it back. Required: it is the justification.</summary>
    public string Description { get; set; } = null!;

    public RefundStatus Status { get; set; } = RefundStatus.Requested;

    public Guid? ReviewedByUserId { get; set; }

    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Why it was declined.</summary>
    public string? ReviewNote { get; set; }

    /// <summary>The payroll month it is paid with. Set on approval.</summary>
    public int? PayrollYear { get; set; }

    public int? PayrollMonth { get; set; }
}
