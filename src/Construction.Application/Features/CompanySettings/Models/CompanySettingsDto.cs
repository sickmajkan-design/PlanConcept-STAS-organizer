using System.Linq.Expressions;

namespace Construction.Application.Features.CompanySettings.Models;

/// <summary>
/// The full company profile, for any authenticated user — nothing here is
/// sensitive to a company's own staff, only writing it is restricted.
/// </summary>
public class CompanySettingsDto
{
    public string? Name { get; init; }

    public string? Address { get; init; }

    public string? TaxId { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? VatNumber { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? WeeklyReportsForwardEmail { get; init; }

    public bool HasLogo { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// What the pre-login screen needs and nothing more. Reached with no
/// Authorization header at all, so address/tax/contact fields must never
/// appear here.
/// </summary>
public class PublicCompanyBrandingDto
{
    public string? Name { get; init; }

    public bool HasLogo { get; init; }
}

public static class CompanySettingsMapping
{
    public static Expression<Func<Domain.Entities.CompanySettings, CompanySettingsDto>> Projection =>
        settings => new CompanySettingsDto
        {
            Name = settings.Name,
            Address = settings.Address,
            TaxId = settings.TaxId,
            RegistrationNumber = settings.RegistrationNumber,
            VatNumber = settings.VatNumber,
            Phone = settings.Phone,
            Email = settings.Email,
            WeeklyReportsForwardEmail = settings.WeeklyReportsForwardEmail,
            HasLogo = settings.LogoStorageKey != null,
            UpdatedAt = settings.UpdatedAt,
        };

    public static Expression<Func<Domain.Entities.CompanySettings, PublicCompanyBrandingDto>> BrandingProjection =>
        settings => new PublicCompanyBrandingDto
        {
            Name = settings.Name,
            HasLogo = settings.LogoStorageKey != null,
        };

    /// <summary>The "nobody has configured this yet" answer, not a 404.</summary>
    public static readonly CompanySettingsDto Empty = new();

    public static readonly PublicCompanyBrandingDto EmptyBranding = new();
}
