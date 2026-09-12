using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// The platform owner's own company data — a singleton, not a per-tenant
/// record. There is exactly one row, created the first time a SuperAdmin
/// saves the company profile. Read by every authenticated user (it renders
/// in the sidebar) and, for the name and logo only, by nobody at all — see
/// the anonymous branding endpoint this backs, needed for the pre-login
/// screen.
/// </summary>
public class CompanySettings : BaseEntity, IAuditable
{
    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    /// <summary>Tax identification number — PIB/JIB/whatever the local term is.</summary>
    public string? TaxId { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? VatNumber { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    /// <summary>
    /// Where a submitted weekly site report is also forwarded, in addition
    /// to being filed on the platform. Null means "don't forward" — the
    /// office reviews submissions from the list instead.
    /// </summary>
    public string? WeeklyReportsForwardEmail { get; set; }

    /// <summary>The <see cref="Application.Common.Interfaces.IFileStorage"/> key the logo bytes live under.</summary>
    public string? LogoStorageKey { get; set; }

    /// <summary>Needed to set the right Content-Type header when the logo is served back.</summary>
    public string? LogoContentType { get; set; }
}
