using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One of the operator's own organizational units (poslovna jedinica) — e.g. a regional office.
/// Not to be confused with <see cref="CustomerCompany"/>, which is a legal entity of a client.
/// </summary>
/// <remarks>
/// A project optionally belongs to one branch; vehicles, tools, costs and time entries are
/// attributed to a branch through the project they are assigned to, never directly.
/// A branch can also carry the data of the entity it stands for (see <see cref="Kind"/>), so
/// documents and exports that name it can show who is issuing them.
/// </remarks>
public class Branch : BaseEntity, IAuditable
{
    /// <summary>What people call it: the short name shown in the switcher and the lists.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Hex colour (#RRGGBB) of the dot shown before the branch name in the UI.</summary>
    public string Color { get; set; } = "#3457D5";

    /// <summary>A branch with projects cannot be deleted, only switched off so it is no longer offered.</summary>
    public bool IsActive { get; set; } = true;

    public BranchKind Kind { get; set; } = BranchKind.LegalEntity;

    /// <summary>The full registered name for documents. Falls back to <see cref="Name"/> when empty.</summary>
    public string? LegalName { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    /// <summary>ISO 3166-1 alpha-2 (e.g. "BA", "RS", "DE").</summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Tax identification number — JIB, PIB, OIB, Steuernummer, whatever the unit's country calls
    /// it. Visible only to a SuperAdmin, or to a user a SuperAdmin has explicitly granted
    /// <see cref="User.CanViewCustomerTaxDetails"/>; changed by a SuperAdmin only.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>Company registration number (matični broj). Same access as <see cref="TaxId"/>.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>VAT number, for a unit registered for it. Same access as <see cref="TaxId"/>.</summary>
    public string? VatNumber { get; set; }

    /// <summary>The owner or director.</summary>
    public string? OwnerName { get; set; }

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// The unit this one belongs to (a region above a branch, a branch above an office), or null
    /// when it stands directly under the company. At most <see cref="MaxDepth"/> levels deep.
    /// </summary>
    public Guid? ParentBranchId { get; set; }

    public Branch? Parent { get; set; }

    public ICollection<Branch> Children { get; set; } = new List<Branch>();

    /// <summary>
    /// The ids from the top of the tree down to this unit, each wrapped in commas
    /// (<c>,root,parent,self,</c>). A unit includes everything under it when its id appears in
    /// another unit's path, which is one string test a query can run without joining the tree.
    /// Kept by <c>BranchTree</c>; never set by hand.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The person who runs the unit. Optional; the unit's owner and contact stay as text for documents.</summary>
    public Guid? HeadEmployeeId { get; set; }

    public Employee? HeadEmployee { get; set; }

    /// <summary>Levels a unit may be nested to: a region, a branch, an office.</summary>
    public const int MaxDepth = 3;

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
