using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One legal entity of a <see cref="Customer"/>: a client may have several companies, and an
/// invoice can be issued to one or more of them.
/// </summary>
/// <remarks>
/// A customer with no companies of its own is invoiced as a whole; nothing here is required
/// until a client actually has more than one.
/// </remarks>
public class CustomerCompany : BaseEntity, IAuditable
{
    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Where invoices to this company go.</summary>
    public string? Address { get; set; }

    /// <summary>
    /// A company with invoices cannot be deleted, only switched off so it is no longer offered
    /// for new ones.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
