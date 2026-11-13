using Construction.Application.Features.Privacy.Commands.RequestAccountDeletion;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>A worker asking, from the app, for their data to be erased.</summary>
[Collection(DatabaseCollection.Name)]
public class AccountDeletionRequestTests : IntegrationTestBase
{
    public AccountDeletionRequestTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Request_NotifiesSuperAdminsWithTheWorkersName_AndNobodyElse()
    {
        var (worker, superAdmin, admin) = await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var worker = await TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id);
            var superAdmin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            var admin = await TestData.SeedUserAsync(scope, UserRole.Admin);
            return (worker, superAdmin, admin);
        });

        await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(worker.Id, worker.Role, worker.EmployeeId, worker.Email);
            return scope.Send(new RequestAccountDeletionCommand());
        });

        var notices = await InScope(scope => scope.Db.Notifications
            .Where(n => n.Type == NotificationType.GeneralAnnouncement)
            .ToListAsync());

        var forSuperAdmin = Assert.Single(notices, n => n.UserId == superAdmin.Id);
        Assert.Contains("Zahtjev za brisanje", forSuperAdmin.Title);
        Assert.DoesNotContain(notices, n => n.UserId == admin.Id || n.UserId == worker.Id);
    }
}
