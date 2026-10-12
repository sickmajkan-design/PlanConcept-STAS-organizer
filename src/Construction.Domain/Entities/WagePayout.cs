using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Money paid out to a group of workers for a month: an advance (akontacija), which can happen several
/// times, or the final payment that settles the month.
/// </summary>
/// <remarks>
/// What is left to pay is never stored: it is the group's earnings in the settlement, less advances, less
/// final payments already made, so a correction in the settlement moves it with no second edit.
/// </remarks>
public class WagePayout : BaseEntity, IAuditable
{
    /// <summary>The month of the settlement it belongs to, not the day of payment.</summary>
    public int Year { get; set; }

    public int Month { get; set; }

    public PayoutGroup Group { get; set; }

    /// <summary>Set when it was paid to one person (an advance to a worker); null for a group as a whole.</summary>
    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public PayoutKind Kind { get; set; }

    /// <summary>The day it was paid, or for a final payment not yet made, the day it is planned.</summary>
    public DateOnly PayDate { get; set; }

    /// <summary>Always more than zero.</summary>
    public decimal Amount { get; set; }

    public BookPaymentMethod? Method { get; set; }

    /// <summary>When it actually went out. Null for a final payment that is only planned.</summary>
    public DateOnly? PaidOn { get; set; }

    public string? Note { get; set; }

    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
