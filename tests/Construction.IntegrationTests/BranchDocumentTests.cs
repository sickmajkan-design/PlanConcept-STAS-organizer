using ClosedXML.Excel;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Exports.Queries;
using Construction.Application.Features.Invoices.Commands.CreateInvoice;
using Construction.Application.Features.Invoices.Queries.GetInvoiceDocument;
using Construction.Application.Features.Invoices.Queries.GetInvoices;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// What names a business unit: the printed copy of an invoice carries the unit as its issuer, the
/// invoice list says which unit it belongs to, and the exports show the unit and can be narrowed
/// to it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BranchDocumentTests : IntegrationTestBase
{
    public BranchDocumentTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static async Task<User> SeedAdminAsync(TestScope scope, bool taxGrant)
    {
        var user = await TestData.SeedUserAsync(scope, UserRole.Admin);
        user.FinanceAccess = FinanceAccess.Full;
        user.CanViewCustomerTaxDetails = taxGrant;
        await scope.Db.SaveChangesAsync();

        return user;
    }

    /// <summary>The company profile is a singleton: put a known one in place for the test.</summary>
    private async Task SeedCompanyAsync()
    {
        await InScope(async scope =>
        {
            await scope.Db.CompanySettings.ExecuteDeleteAsync();
            scope.Db.CompanySettings.Add(new CompanySettings
            {
                Name = "Plan Concept d.o.o.",
                Address = "Glavna 1, Beograd",
                TaxId = "100000001",
                RegistrationNumber = "20000002",
                VatNumber = "300000003",
            });
            await scope.Db.SaveChangesAsync();
        });
    }

    private sealed record Seed(Branch Branch, Project Project, Guid InvoiceId, string InvoiceNumber);

    /// <summary>A unit, a client's site in it, and one invoice on that site.</summary>
    private async Task<Seed> SeedInvoiceAsync(Action<Branch> shape)
    {
        return await InScope(async scope =>
        {
            var superAdmin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            superAdmin.FinanceAccess = FinanceAccess.Full;
            await scope.Db.SaveChangesAsync();
            ActAs(scope, superAdmin);

            var branch = new Branch { Name = $"Unit {Unique()}", Color = "#0F8A5F" };
            shape(branch);
            scope.Db.Branches.Add(branch);

            var customer = await TestData.SeedCustomerAsync(scope);
            customer.TaxId = "CLIENT-TAX-1";

            var project = await TestData.SeedProjectAsync(scope);
            project.CustomerId = customer.Id;
            project.BranchId = branch.Id;
            await scope.Db.SaveChangesAsync();

            var number = $"R-{Unique()}";
            var invoice = await scope.Send(new CreateInvoiceCommand
            {
                ProjectId = project.Id,
                Number = number,
                IssueDate = new DateOnly(2026, 9, 20),
                Amount = 1250m,
            });

            return new Seed(branch, project, invoice.Id, number);
        });
    }

    [Fact]
    public async Task The_printed_copy_names_the_unit_that_issues_it()
    {
        await SeedCompanyAsync();
        var seed = await SeedInvoiceAsync(b =>
        {
            b.LegalName = "Plan Concept BH d.o.o.";
            b.Address = "Zmaja od Bosne 1";
            b.City = "Sarajevo";
            b.TaxId = "4200000000001";
        });

        var document = await InScope(async scope =>
        {
            ActAs(scope, await TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
            return await scope.Send(new GetInvoiceDocumentQuery(seed.InvoiceId));
        });

        Assert.Equal(seed.InvoiceNumber, document.Number);
        Assert.Equal(1250m, document.Amount);
        Assert.Equal("Plan Concept BH d.o.o.", document.Issuer.Name);
        Assert.Equal("Sarajevo", document.Issuer.City);
        Assert.Equal("4200000000001", document.Issuer.TaxId);
        Assert.Equal("CLIENT-TAX-1", Assert.Single(document.Recipients).TaxId);
    }

    [Fact]
    public async Task A_representative_office_prints_the_companys_numbers_and_says_so()
    {
        await SeedCompanyAsync();
        var seed = await SeedInvoiceAsync(b =>
        {
            b.Kind = BranchKind.RepresentativeOffice;
            b.City = "Berlin";
        });

        var document = await InScope(async scope =>
        {
            ActAs(scope, await TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
            return await scope.Send(new GetInvoiceDocumentQuery(seed.InvoiceId));
        });

        Assert.Equal("Plan Concept d.o.o.", document.Issuer.Name);
        Assert.Equal("100000001", document.Issuer.TaxId);
        Assert.True(document.Issuer.UsesCompanyNumbers);
        Assert.Equal("Berlin", document.Issuer.City);
    }

    [Fact]
    public async Task Without_the_tax_right_the_copy_carries_no_tax_numbers_at_all()
    {
        await SeedCompanyAsync();
        var seed = await SeedInvoiceAsync(b => b.TaxId = "4200000000001");

        var document = await InScope(async scope =>
        {
            ActAs(scope, await SeedAdminAsync(scope, taxGrant: false));
            return await scope.Send(new GetInvoiceDocumentQuery(seed.InvoiceId));
        });

        Assert.Null(document.Issuer.TaxId);
        Assert.Null(Assert.Single(document.Recipients).TaxId);
        Assert.Equal(seed.InvoiceNumber, document.Number);
    }

    [Fact]
    public async Task A_project_with_no_unit_prints_the_company_as_issuer()
    {
        await SeedCompanyAsync();

        var invoiceId = await InScope(async scope =>
        {
            var superAdmin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            superAdmin.FinanceAccess = FinanceAccess.Full;
            await scope.Db.SaveChangesAsync();
            ActAs(scope, superAdmin);

            var project = await TestData.SeedProjectAsync(scope);
            var invoice = await scope.Send(new CreateInvoiceCommand
            {
                ProjectId = project.Id,
                Number = $"R-{Unique()}",
                IssueDate = new DateOnly(2026, 9, 21),
                Amount = 10m,
            });

            return invoice.Id;
        });

        var document = await InScope(async scope =>
        {
            ActAs(scope, await TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
            return await scope.Send(new GetInvoiceDocumentQuery(invoiceId));
        });

        Assert.Equal("Plan Concept d.o.o.", document.Issuer.Name);
        Assert.Null(document.Issuer.BranchId);
    }

    [Fact]
    public async Task The_document_needs_management_with_the_finance_grant_and_an_existing_invoice()
    {
        var seed = await SeedInvoiceAsync(_ => { });

        await InScope(async scope =>
        {
            var foreman = await TestData.SeedUserAsync(scope, UserRole.Foreman);
            ActAs(scope, foreman);

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => scope.Send(new GetInvoiceDocumentQuery(seed.InvoiceId)));

            var superAdmin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            ActAs(scope, superAdmin);

            await Assert.ThrowsAsync<NotFoundException>(() => scope.Send(new GetInvoiceDocumentQuery(Guid.NewGuid())));
        });
    }

    [Fact]
    public async Task The_invoice_list_says_which_unit_an_invoice_belongs_to()
    {
        var seed = await SeedInvoiceAsync(_ => { });

        var page = await InScope(async scope =>
        {
            ActAs(scope, await SeedAdminAsync(scope, taxGrant: false));
            return await scope.Send(new GetInvoicesQuery { BranchId = seed.Branch.Id, PageSize = 50 });
        });

        var row = Assert.Single(page.Items);
        Assert.Equal(seed.Branch.Name, row.BranchName);
        Assert.Equal("#0F8A5F", row.BranchColor);
    }

    private static IXLWorksheet Open(ExportFile file)
    {
        using var stream = new MemoryStream(file.Content);

        return new XLWorkbook(stream).Worksheets.First();
    }

    [Fact]
    public async Task The_projects_export_shows_the_unit_in_a_last_column_and_can_be_narrowed_to_it()
    {
        var inside = Unique();
        var outside = Unique();

        var branch = await InScope(async scope =>
        {
            var b = new Branch { Name = $"Export {Unique()}", Color = "#7C3AED" };
            scope.Db.Branches.Add(b);

            var one = await TestData.SeedProjectAsync(scope, $"In {inside}");
            one.BranchId = b.Id;
            await TestData.SeedProjectAsync(scope, $"Out {outside}");
            await scope.Db.SaveChangesAsync();

            return b;
        });

        var narrowed = await InScope(async scope =>
        {
            ActAs(scope, await TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
            return Open(await scope.Send(new ExportProjectsQuery { BranchId = branch.Id }));
        });

        var last = narrowed.Row(1).LastCellUsed()!.Address.ColumnNumber;

        Assert.Equal("Poslovna jedinica", narrowed.Cell(1, last).GetString());
        Assert.Equal($"In {inside}", narrowed.Cell(2, 1).GetString());
        Assert.Equal(branch.Name, narrowed.Cell(2, last).GetString());

        // Only that unit's project: the next row is the generated-at footer, not another project.
        Assert.DoesNotContain(
            Enumerable.Range(2, narrowed.LastRowUsed()!.RowNumber() - 1),
            row => narrowed.Cell(row, 1).GetString() == $"Out {outside}");
    }
}
