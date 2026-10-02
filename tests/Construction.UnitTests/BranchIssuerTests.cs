using Construction.Application.Features.Branches;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.UnitTests;

/// <summary>
/// Who is named as the issuer of a document: a unit's own data, or the company's where the unit has
/// none of its own — and never another entity's numbers on a separate legal entity.
/// </summary>
public class BranchIssuerTests
{
    private static CompanySettings Company() => new()
    {
        Name = "Plan Concept d.o.o.",
        Address = "Glavna 1, Beograd",
        TaxId = "100000001",
        RegistrationNumber = "20000002",
        VatNumber = "300000003",
        Phone = "+381 11 000",
        Email = "info@company.example",
    };

    private static Branch Entity() => new()
    {
        Name = "Sarajevo",
        Kind = BranchKind.LegalEntity,
        LegalName = "Plan Concept BH d.o.o.",
        Address = "Zmaja od Bosne 1",
        City = "Sarajevo",
        PostalCode = "71000",
        CountryCode = "BA",
        TaxId = "4200000000001",
        OwnerName = "Ime Prezime",
    };

    [Fact]
    public void Without_a_unit_the_company_is_the_issuer()
    {
        var issuer = BranchIssuer.Resolve(null, Company(), includeTaxDetails: true);

        Assert.Equal("Plan Concept d.o.o.", issuer.Name);
        Assert.Equal("100000001", issuer.TaxId);
        Assert.Null(issuer.BranchId);
        Assert.False(issuer.UsesCompanyNumbers);
    }

    [Fact]
    public void A_legal_entity_uses_its_own_registered_name_and_numbers()
    {
        var issuer = BranchIssuer.Resolve(Entity(), Company(), includeTaxDetails: true);

        Assert.Equal("Plan Concept BH d.o.o.", issuer.Name);
        Assert.Equal("Sarajevo", issuer.BranchName);
        Assert.Equal("4200000000001", issuer.TaxId);
        Assert.Equal("Zmaja od Bosne 1", issuer.Address);
        Assert.False(issuer.UsesCompanyNumbers);
    }

    [Fact]
    public void A_legal_entity_never_borrows_the_companys_numbers()
    {
        // Another entity's registration number on its document would be wrong, so blank stays blank.
        var issuer = BranchIssuer.Resolve(Entity(), Company(), includeTaxDetails: true);

        Assert.Null(issuer.RegistrationNumber);
        Assert.Null(issuer.VatNumber);
    }

    [Fact]
    public void A_legal_entity_without_a_registered_name_is_named_by_its_short_name()
    {
        var branch = Entity();
        branch.LegalName = null;

        Assert.Equal("Sarajevo", BranchIssuer.Resolve(branch, Company(), includeTaxDetails: true).Name);
    }

    [Fact]
    public void A_representative_office_uses_the_companys_name_and_numbers_where_it_has_none()
    {
        var office = new Branch
        {
            Name = "Berlin",
            Kind = BranchKind.RepresentativeOffice,
            Address = "Unter den Linden 1",
            City = "Berlin",
            CountryCode = "DE",
        };

        var issuer = BranchIssuer.Resolve(office, Company(), includeTaxDetails: true);

        Assert.Equal("Plan Concept d.o.o.", issuer.Name);
        Assert.Equal("100000001", issuer.TaxId);
        Assert.Equal("20000002", issuer.RegistrationNumber);
        Assert.Equal("300000003", issuer.VatNumber);
        Assert.True(issuer.UsesCompanyNumbers);

        // Its own address is still its own: the office is where the document comes from.
        Assert.Equal("Unter den Linden 1", issuer.Address);
        Assert.Equal("Berlin", issuer.BranchName);
    }

    [Fact]
    public void A_representative_office_keeps_a_number_it_has_of_its_own()
    {
        var office = new Branch
        {
            Name = "Berlin",
            Kind = BranchKind.RepresentativeOffice,
            VatNumber = "DE123456789",
        };

        var issuer = BranchIssuer.Resolve(office, Company(), includeTaxDetails: true);

        Assert.Equal("DE123456789", issuer.VatNumber);
        Assert.Equal("100000001", issuer.TaxId);
    }

    [Fact]
    public void Tax_numbers_are_left_out_for_someone_who_may_not_see_them()
    {
        var office = new Branch { Name = "Berlin", Kind = BranchKind.RepresentativeOffice };

        foreach (var issuer in new[]
        {
            BranchIssuer.Resolve(null, Company(), includeTaxDetails: false),
            BranchIssuer.Resolve(Entity(), Company(), includeTaxDetails: false),
            BranchIssuer.Resolve(office, Company(), includeTaxDetails: false),
        })
        {
            Assert.Null(issuer.TaxId);
            Assert.Null(issuer.RegistrationNumber);
            Assert.Null(issuer.VatNumber);
            Assert.False(issuer.UsesCompanyNumbers);
        }
    }

    [Fact]
    public void Without_any_company_profile_a_unit_still_resolves()
    {
        var issuer = BranchIssuer.Resolve(Entity(), null, includeTaxDetails: true);

        Assert.Equal("Plan Concept BH d.o.o.", issuer.Name);
    }
}
