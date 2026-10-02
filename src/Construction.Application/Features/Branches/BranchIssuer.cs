using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Branches;

/// <summary>Who stands behind a document: the name, address and numbers it carries as its issuer.</summary>
public sealed record IssuerDto
{
    /// <summary>The registered name to print — the unit's own, or the company's.</summary>
    public string Name { get; init; } = string.Empty;

    public Guid? BranchId { get; init; }

    /// <summary>The short name of the unit; null when the document is the company's own.</summary>
    public string? BranchName { get; init; }

    public BranchKind? Kind { get; init; }

    public string? Address { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string? CountryCode { get; init; }

    /// <summary>Tax numbers are null when the caller may not see them, see <c>CustomerRules</c>.</summary>
    public string? TaxId { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? VatNumber { get; init; }

    public string? OwnerName { get; init; }

    public string? ContactPerson { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    /// <summary>True when the numbers shown are the company's, because the unit is an office that has none of its own.</summary>
    public bool UsesCompanyNumbers { get; init; }
}

/// <summary>
/// Works out the issuer of a document from a business unit, so every document and export that
/// names a unit shows the same data in the same way.
/// </summary>
/// <remarks>
/// A unit with no data of its own, or no unit at all, is the company itself (Podaci firme). A
/// separate legal entity shows what it has and nothing borrowed: another entity's number on its
/// document would be wrong. A representative office is not a legal entity, so what it leaves blank
/// — its registered name and its numbers — is the company's.
/// </remarks>
public static class BranchIssuer
{
    public static IssuerDto Resolve(Branch? branch, Domain.Entities.CompanySettings? company, bool includeTaxDetails)
    {
        if (branch is null)
        {
            return FromCompany(company, includeTaxDetails);
        }

        var isOffice = branch.Kind == BranchKind.RepresentativeOffice;

        var taxId = branch.TaxId;
        var registration = branch.RegistrationNumber;
        var vat = branch.VatNumber;
        var borrowed = false;

        if (isOffice && company is not null)
        {
            borrowed = (taxId is null && company.TaxId is not null)
                || (registration is null && company.RegistrationNumber is not null)
                || (vat is null && company.VatNumber is not null);

            taxId ??= company.TaxId;
            registration ??= company.RegistrationNumber;
            vat ??= company.VatNumber;
        }

        var name = branch.LegalName
            ?? (isOffice && !string.IsNullOrWhiteSpace(company?.Name) ? company!.Name : branch.Name);

        return new IssuerDto
        {
            Name = name,
            BranchId = branch.Id,
            BranchName = branch.Name,
            Kind = branch.Kind,
            Address = branch.Address,
            City = branch.City,
            PostalCode = branch.PostalCode,
            CountryCode = branch.CountryCode,
            TaxId = includeTaxDetails ? taxId : null,
            RegistrationNumber = includeTaxDetails ? registration : null,
            VatNumber = includeTaxDetails ? vat : null,
            OwnerName = branch.OwnerName,
            ContactPerson = branch.ContactPerson,
            Phone = branch.Phone,
            Email = branch.Email,
            UsesCompanyNumbers = includeTaxDetails && borrowed,
        };
    }

    private static IssuerDto FromCompany(Domain.Entities.CompanySettings? company, bool includeTaxDetails) => new()
    {
        Name = company?.Name ?? string.Empty,
        Address = company?.Address,
        TaxId = includeTaxDetails ? company?.TaxId : null,
        RegistrationNumber = includeTaxDetails ? company?.RegistrationNumber : null,
        VatNumber = includeTaxDetails ? company?.VatNumber : null,
        Phone = company?.Phone,
        Email = company?.Email,
    };

    /// <summary>The issuer for a unit (or none), loaded from the database.</summary>
    public static async Task<IssuerDto> ResolveAsync(
        IApplicationDbContext context,
        Guid? branchId,
        bool includeTaxDetails,
        CancellationToken cancellationToken)
    {
        var company = await context.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        var branch = branchId is { } id
            ? await context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            : null;

        return Resolve(branch, company, includeTaxDetails);
    }
}
