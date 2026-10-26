using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Customers.Commands.CreateCustomer;
using Construction.Application.Features.Customers.Commands.UpdateCustomer;
using Construction.Application.Features.Customers.Queries.GetCustomers;
using Construction.Application.Features.Planning;
using Construction.Application.Features.Projects.Queries.GetProjects;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The country a customer is from: stored in capitals, refused when it is not a two-letter code, carried on the
/// projects and on the plan, and usable as a filter. The site has its own code; the two are not the same thing.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CustomerCountryTests : IntegrationTestBase
{
    public CustomerCountryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task The_country_is_kept_in_capitals_and_can_be_cleared()
    {
        var created = await InScopeAs(UserRole.Admin, scope => scope.Send(new CreateCustomerCommand { Name = $"Roche {Unique()}", CountryCode = "ch" }));
        Assert.Equal("CH", created.CountryCode);

        var cleared = await InScopeAs(UserRole.Admin, scope => scope.Send(new UpdateCustomerCommand { Id = created.Id, Name = created.Name, CountryCode = "  " }));
        Assert.Null(cleared.CountryCode);
    }

    [Theory]
    [InlineData("SWISS")]
    [InlineData("C")]
    [InlineData("1A")]
    public async Task Anything_but_two_letters_is_refused(string code)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            InScopeAs(UserRole.Admin, scope => scope.Send(new CreateCustomerCommand { Name = $"Roche {Unique()}", CountryCode = code })));
    }

    [Fact]
    public async Task A_project_and_the_plan_carry_the_country_of_the_customer_apart_from_the_site()
    {
        var customer = await InScopeAs(UserRole.Admin, scope => scope.Send(new CreateCustomerCommand { Name = $"Roche {Unique()}", CountryCode = "CH" }));
        var site = await InScope(async scope =>
        {
            var p = await TestData.SeedProjectAsync(scope, countryCode: "DE");
            p.CustomerId = customer.Id;
            await scope.Db.SaveChangesAsync();
            return p;
        });

        var page = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetProjectsQuery { PageSize = 100, CustomerId = customer.Id }));
        var row = Assert.Single(page.Items);
        Assert.Equal("CH", row.CustomerCountryCode);
        Assert.Equal("DE", row.CountryCode);

        var plan = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningQuery { From = new DateOnly(2031, 6, 1), To = new DateOnly(2031, 6, 30) }));
        var planned = plan.Projects.Single(p => p.Id == site.Id);
        Assert.Equal("CH", planned.CustomerCountryCode);
        Assert.Equal("DE", planned.CountryCode);
    }

    [Fact]
    public async Task Projects_and_customers_can_be_filtered_by_country_and_by_having_none()
    {
        var swiss = await InScopeAs(UserRole.Admin, scope => scope.Send(new CreateCustomerCommand { Name = $"Swiss {Unique()}", CountryCode = "CH" }));
        var nowhere = await InScopeAs(UserRole.Admin, scope => scope.Send(new CreateCustomerCommand { Name = $"Nowhere {Unique()}" }));
        var (swissSite, nowhereSite) = await InScope(async scope =>
        {
            var a = await TestData.SeedProjectAsync(scope);
            var b = await TestData.SeedProjectAsync(scope);
            a.CustomerId = swiss.Id;
            b.CustomerId = nowhere.Id;
            await scope.Db.SaveChangesAsync();
            return (a, b);
        });

        var swissProjects = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetProjectsQuery { PageSize = 100, CustomerCountry = "ch" }));
        Assert.Contains(swissProjects.Items, p => p.Id == swissSite.Id);
        Assert.DoesNotContain(swissProjects.Items, p => p.Id == nowhereSite.Id);

        var none = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetProjectsQuery { PageSize = 100, CustomerCountry = "none" }));
        Assert.Contains(none.Items, p => p.Id == nowhereSite.Id);
        Assert.DoesNotContain(none.Items, p => p.Id == swissSite.Id);

        var customers = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetCustomersQuery { PageSize = 100, Country = "none" }));
        Assert.Contains(customers.Items, c => c.Id == nowhere.Id);
        Assert.DoesNotContain(customers.Items, c => c.Id == swiss.Id);
    }
}
