using Construction.Application.Features.Customers.Commands.CreateCustomer;
using Construction.Application.Features.Customers.Commands.UpdateCustomer;
using Construction.Application.Features.Customers.Queries.GetCustomerById;
using Construction.Application.Features.Customers.Queries.GetCustomers;
using Construction.Application.Features.Users.Commands.UpdateUser;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A customer's tax ID, registration number and VAT number are the one part
/// of the record that is not simply office work like the rest of it — a
/// SuperAdmin always sees and edits them, and everyone else only once a
/// SuperAdmin has explicitly granted <c>User.CanViewCustomerTaxDetails</c>.
/// These run against PostgreSQL because the thing worth proving is that an
/// unpermitted caller's response never carries the value at all, which only
/// a real round trip through the projection can show.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CustomerTaxDetailsTests : IntegrationTestBase
{
    public CustomerTaxDetailsTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static CreateCustomerCommand NewCommand(string name) => new()
    {
        Name = name,
        TaxId = "JIB-123",
        RegistrationNumber = "MB-456",
        VatNumber = "VAT-789",
    };

    [Fact]
    public async Task A_super_admin_sees_the_tax_details_they_just_set()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var created = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(NewCommand("Customer A"));
        });

        Assert.Equal("JIB-123", created.TaxId);
        Assert.Equal("MB-456", created.RegistrationNumber);
        Assert.Equal("VAT-789", created.VatNumber);
    }

    [Fact]
    public async Task An_admin_with_no_grant_never_receives_the_values()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var created = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(NewCommand("Customer B"));
        });

        var seenByAdmin = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetCustomerByIdQuery(created.Id));
        });

        Assert.Null(seenByAdmin.TaxId);
        Assert.Null(seenByAdmin.RegistrationNumber);
        Assert.Null(seenByAdmin.VatNumber);
    }

    [Fact]
    public async Task Granting_the_flag_lets_that_admin_see_the_values()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var created = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(NewCommand("Customer C"));
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new UpdateUserCommand
            {
                Id = admin.Id,
                Email = admin.Email,
                Role = admin.Role,
                CanViewCustomerTaxDetails = true,
            });
        });

        var seenByAdmin = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetCustomerByIdQuery(created.Id));
        });

        Assert.Equal("JIB-123", seenByAdmin.TaxId);
        Assert.Equal("MB-456", seenByAdmin.RegistrationNumber);
        Assert.Equal("VAT-789", seenByAdmin.VatNumber);
    }

    [Fact]
    public async Task An_admin_cannot_grant_the_flag_to_themselves_or_anyone_else()
    {
        // Only a SuperAdmin caller may actually change the grant — an Admin's
        // own request to flip it is silently ignored rather than refused, so
        // the same request can still change a user's ordinary fields.
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var other = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new UpdateUserCommand
            {
                Id = other.Id,
                Email = other.Email,
                Role = other.Role,
                CanViewCustomerTaxDetails = true,
            });
        });

        var stillUngranted = await InScope(scope => scope.Db.Users
            .Where(u => u.Id == other.Id)
            .Select(u => u.CanViewCustomerTaxDetails)
            .SingleAsync());

        Assert.False(stillUngranted);
    }

    [Fact]
    public async Task An_admin_without_the_grant_cannot_set_tax_details_either()
    {
        // Submitted values from a caller who may not edit them are silently
        // dropped, not refused — the customer's ordinary fields still save.
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var created = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(NewCommand("Customer D"));
        });

        // The command handler itself withheld what it just wrote (Admin has
        // no grant), so the create response already shows nulls — confirmed
        // directly against the database, which is the actual source of truth.
        var stored = await InScope(scope => scope.Db.Customers
            .Where(c => c.Id == created.Id)
            .Select(c => new { c.TaxId, c.RegistrationNumber, c.VatNumber })
            .SingleAsync());

        Assert.Null(stored.TaxId);
        Assert.Null(stored.RegistrationNumber);
        Assert.Null(stored.VatNumber);
    }

    [Fact]
    public async Task A_super_admin_can_update_tax_details_and_an_admin_cannot_overwrite_them()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var created = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(NewCommand("Customer E"));
        });

        // The admin's update carries different tax values, but has no grant
        // to change them — they must not overwrite what the SuperAdmin set.
        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new UpdateCustomerCommand
            {
                Id = created.Id,
                Name = "Customer E Renamed",
                TaxId = "SMUGGLED",
                RegistrationNumber = "SMUGGLED",
                VatNumber = "SMUGGLED",
            });
        });

        var stored = await InScope(scope => scope.Db.Customers
            .Where(c => c.Id == created.Id)
            .Select(c => new { c.Name, c.TaxId, c.RegistrationNumber, c.VatNumber })
            .SingleAsync());

        Assert.Equal("Customer E Renamed", stored.Name);
        Assert.Equal("JIB-123", stored.TaxId);
        Assert.Equal("MB-456", stored.RegistrationNumber);
        Assert.Equal("VAT-789", stored.VatNumber);
    }

    [Fact]
    public async Task The_customer_list_also_withholds_tax_details_from_an_ungranted_caller()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(NewCommand("Customer F Uniquetaxsearch"));
        });

        var page = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetCustomersQuery { Search = "Uniquetaxsearch", PageSize = 100 });
        });

        var row = Assert.Single(page.Items);
        Assert.Null(row.TaxId);
        Assert.Null(row.RegistrationNumber);
        Assert.Null(row.VatNumber);
    }
}
