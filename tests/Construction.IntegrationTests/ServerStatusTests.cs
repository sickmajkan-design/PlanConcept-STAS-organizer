using Construction.Application.Common.Exceptions;
using Construction.Application.Features.SystemStatus;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The server status page for the Super Admin: the machine, the application, the database and what the platform leans on,
/// read live. Nobody else may see it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ServerStatusTests : IntegrationTestBase
{
    public ServerStatusTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task The_super_admin_sees_the_application_the_database_and_the_services()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var status = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(owner.Id, owner.Role, null, owner.Email);
            return scope.Send(new GetServerStatusQuery());
        });

        // The application: a process that has been running for a moment, in a known runtime.
        Assert.False(string.IsNullOrWhiteSpace(status.Application.Version));
        Assert.StartsWith(".NET", status.Application.Runtime);
        Assert.True(status.Application.ProcessMemoryBytes > 0);
        Assert.True(status.Application.Threads > 0);
        Assert.True(status.Application.UptimeSeconds >= 0);

        // The database answered, and says how big it is and how busy.
        Assert.True(status.Database.Reachable);
        Assert.StartsWith("PostgreSQL", status.Database.Version);
        Assert.True(status.Database.SizeBytes > 0);
        Assert.True(status.Database.Connections >= 1);
        Assert.True(status.Database.MaxConnections > 0);
        Assert.NotEmpty(status.Database.LargestTables);
        Assert.True(status.Database.LargestTables.SequenceEqual(status.Database.LargestTables.OrderByDescending(t => t.Bytes)),
            "the biggest table comes first");

        // The host: at least the number of cores, whatever else the operating system lets through.
        Assert.True(status.Host.Cores > 0);
        Assert.False(string.IsNullOrWhiteSpace(status.Host.Os));

        // The services: the test host has a scanner switched on and keeps files on the disk.
        Assert.True(status.Services.VirusScannerEnabled);
        Assert.True(status.Services.VirusScannerReachable);
        Assert.Equal("disk", status.Services.FileStorage);
        Assert.True(status.Services.OutboxPending >= 0);
        Assert.True(status.Services.OutboxAbandoned >= 0);

        // The disk and the documents come with it, so the page needs one request.
        Assert.True(status.Storage.Known);
    }

    [Fact]
    public async Task A_scanner_that_is_down_shows_as_down()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        try
        {
            await InScope(scope =>
            {
                scope.VirusScanner.Down = true;
                return Task.CompletedTask;
            });

            var status = await InScope(scope =>
            {
                scope.CurrentUser.SignInAs(owner.Id, owner.Role, null, owner.Email);
                return scope.Send(new GetServerStatusQuery());
            });

            Assert.True(status.Services.VirusScannerEnabled);
            Assert.False(status.Services.VirusScannerReachable);
        }
        finally
        {
            await InScope(scope =>
            {
                scope.VirusScanner.Down = false;
                return Task.CompletedTask;
            });
        }
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    [InlineData(UserRole.Worker)]
    public async Task Nobody_but_the_super_admin_may_see_it(UserRole role)
    {
        var someone = await InScope(scope => TestData.SeedUserAsync(scope, role));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            scope.CurrentUser.SignInAs(someone.Id, someone.Role, null, someone.Email);
            return scope.Send(new GetServerStatusQuery());
        }));
    }
}
