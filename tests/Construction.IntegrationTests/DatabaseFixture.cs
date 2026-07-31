using Construction.Application;
using Construction.Application.Common.Interfaces;
using Construction.Infrastructure;
using Construction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Construction.IntegrationTests;

/// <summary>
/// Spins up a throwaway PostgreSQL database for the test run, applies the real
/// migrations to it, and builds the same dependency graph the API uses.
///
/// PostgreSQL rather than an in-memory provider on purpose: the behaviour worth
/// testing here is PostgreSQL-specific — filtered unique indexes that let a
/// soft-deleted identifier be reused, a check constraint on stock quantity, and
/// a conditional UPDATE that must be atomic under concurrency. An in-memory
/// provider would pass these tests while the real database failed.
///
/// Point it at a server with <c>ConstructionTests__Postgres</c>, e.g.
/// <c>Host=localhost;Port=5432;Username=postgres;Password=postgres</c>.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string EnvironmentVariable = "ConstructionTests__Postgres";

    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    private readonly string _databaseName =
        $"construction_test_{Guid.NewGuid():N}";

    private ServiceProvider _services = null!;

    public string AdminConnectionString { get; } =
        Environment.GetEnvironmentVariable(EnvironmentVariable) ?? DefaultAdminConnectionString;

    private string TestConnectionString =>
        new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = _databaseName }
            .ConnectionString;

    public async Task InitializeAsync()
    {
        await CreateDatabaseAsync();

        _services = BuildServiceProvider();

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();

        // Pooled connections would keep the database busy and block the drop.
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(MaintenanceConnectionString());
        await connection.OpenAsync();

        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// A scope per logical request, mirroring how the API resolves handlers.
    /// </summary>
    public TestScope CreateScope() => new(_services.CreateScope());

    private string MaintenanceConnectionString() =>
        new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = "postgres" }
            .ConnectionString;

    private async Task CreateDatabaseAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(MaintenanceConnectionString());
            await connection.OpenAsync();

            await using var create = new NpgsqlCommand(
                $"CREATE DATABASE \"{_databaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException exception)
        {
            throw new InvalidOperationException(
                "These integration tests need a reachable PostgreSQL server. Start one " +
                "(docker compose up postgres) or point the tests at another server with " +
                $"the {EnvironmentVariable} environment variable. Tried: " +
                $"{new NpgsqlConnectionStringBuilder(AdminConnectionString) { Password = "***" }.ConnectionString}",
                exception);
        }
    }

    private ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["JwtSettings:Issuer"] = "construction-api-tests",
                ["JwtSettings:Audience"] = "construction-clients-tests",
                ["JwtSettings:SecretKey"] = "integration-test-signing-key-at-least-32-chars",
                ["JwtSettings:AccessTokenLifetimeMinutes"] = "15",
                ["JwtSettings:RefreshTokenLifetimeDays"] = "7",
                // Left unconfigured on purpose: the email and push senders log
                // instead of reaching the network, so no test touches either.
                ["ClientApp:PasswordResetUrl"] = "https://admin.example.test/reset-password"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Supplied by the API layer in production, from the JWT.
        services.AddScoped<TestCurrentUserService>();
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<TestCurrentUserService>());

        // Replaces Infrastructure's real clock so expiry paths are reachable.
        services.AddSingleton<MutableDateTimeProvider>();
        services.AddSingleton<IDateTimeProvider>(sp => sp.GetRequiredService<MutableDateTimeProvider>());

        return services.BuildServiceProvider();
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}
