using Construction.Application.Common.Exceptions;
using Construction.Application.Features.CustomerPortal.Queries.GetMyCustomerProjects;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The one thing a customer login may see: their own project's status.
/// </summary>
/// <remarks>
/// <see cref="ApiAuthorizationTests"/> already proves a customer login is
/// refused everywhere else. This proves the one door it may open shows only
/// what it should — its own projects, never another customer's, and never a
/// price.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class CustomerPortalTests : IntegrationTestBase
{
    public CustomerPortalTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, customerId: user.CustomerId);

    [Fact]
    public async Task A_customer_sees_only_their_own_projects()
    {
        var (customer, otherCustomer) = await InScope(async scope =>
        {
            var mine = await TestData.SeedCustomerAsync(scope, "Mine d.o.o.");
            var theirs = await TestData.SeedCustomerAsync(scope, "Theirs d.o.o.");
            return (mine, theirs);
        });

        var myProject = await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope, "My Site");
            project.CustomerId = customer.Id;
            await scope.Db.SaveChangesAsync();
            return project;
        });

        await InScope(async scope =>
        {
            var otherProject = await TestData.SeedProjectAsync(scope, "Their Site");
            otherProject.CustomerId = otherCustomer.Id;
            await scope.Db.SaveChangesAsync();
        });

        var user = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Customer, customerId: customer.Id));

        var projects = await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new GetMyCustomerProjectsQuery());
        });

        var row = Assert.Single(projects);
        Assert.Equal(myProject.Id, row.ProjectId);
        Assert.Equal("My Site", row.ProjectName);
    }

    [Fact]
    public async Task A_customers_progress_is_the_share_of_work_items_actually_done()
    {
        var customer = await InScope(scope => TestData.SeedCustomerAsync(scope));

        var project = await InScope(async scope =>
        {
            var p = await TestData.SeedProjectAsync(scope, "Progress Site");
            p.CustomerId = customer.Id;
            await scope.Db.SaveChangesAsync();
            return p;
        });

        await InScope(async scope =>
        {
            scope.Db.WorkItems.AddRange(
                new WorkItem { Title = "Done 1", ProjectId = project.Id, Status = WorkItemStatus.Closed },
                new WorkItem { Title = "Done 2", ProjectId = project.Id, Status = WorkItemStatus.Resolved },
                new WorkItem { Title = "Open", ProjectId = project.Id, Status = WorkItemStatus.Open },
                new WorkItem { Title = "Cancelled", ProjectId = project.Id, Status = WorkItemStatus.Cancelled });
            await scope.Db.SaveChangesAsync();
        });

        var user = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Customer, customerId: customer.Id));

        var projects = await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new GetMyCustomerProjectsQuery());
        });

        var row = Assert.Single(projects);
        // 2 done out of 3 counted work items — the cancelled one is neither
        // progress nor a reason the total never reaches 100%.
        Assert.Equal(67, row.PercentComplete);
    }

    [Fact]
    public async Task An_account_with_no_customer_cannot_use_the_portal()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role);
            return scope.Send(new GetMyCustomerProjectsQuery());
        }));
    }
}
