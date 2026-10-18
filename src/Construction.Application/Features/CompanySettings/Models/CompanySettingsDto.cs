using System.Linq.Expressions;
using Construction.Application.Common;

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

    /// <summary>The organisation's time zone, as an IANA id: the one the platform keeps its calendar in.</summary>
    public string TimeZoneId { get; init; } = CompanyTime.ZoneId;

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

    /// <summary>
    /// The organisation's time zone. Not sensitive, and needed before sign-in has finished: the panel and the app
    /// take their idea of "today" from it.
    /// </summary>
    public string TimeZoneId { get; init; } = CompanyTime.ZoneId;
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
            TimeZoneId = settings.TimeZoneId ?? CompanyTime.ZoneId,
            UpdatedAt = settings.UpdatedAt,
        };

    public static Expression<Func<Domain.Entities.CompanySettings, PublicCompanyBrandingDto>> BrandingProjection =>
        settings => new PublicCompanyBrandingDto
        {
            Name = settings.Name,
            HasLogo = settings.LogoStorageKey != null,
            TimeZoneId = settings.TimeZoneId ?? CompanyTime.ZoneId,
        };

    /// <summary>The "nobody has configured this yet" answer, not a 404.</summary>
    // Properties, not fields: the zone in them is the one in force when they are asked for, not when the type loaded.
    public static CompanySettingsDto Empty => new();

    public static PublicCompanyBrandingDto EmptyBranding => new();
}
