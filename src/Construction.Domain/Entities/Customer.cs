using Construction.Domain.Common;

namespace Construction.Domain.Entities;

public class Customer : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// Tax identification number — JIB, PIB, OIB, Steuernummer, whatever the
    /// customer's own country calls it. Visible only to a SuperAdmin, or to
    /// a user a SuperAdmin has explicitly granted
    /// <see cref="User.CanViewCustomerTaxDetails"/>.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>Business/company registration number (matični broj). Same visibility as <see cref="TaxId"/>.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>VAT number, for a customer registered for it. Same visibility as <see cref="TaxId"/>.</summary>
    public string? VatNumber { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
