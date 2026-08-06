using Construction.Application;
using Construction.Application.Common.Interfaces;
using Construction.Infrastructure;
using Construction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Construction.IntegrationTests;

/// <summary>
/// Spins up a throwaway PostgreSQL database for the test run, applies the real
/// migrations to it, and builds the same dependency graph the API uses.
///
/// The handlers are reached through MediatR rather than over HTTP, so this
/// fixture stops at the application layer. What sits above it — the
/// <c>[Authorize]</c> policies, model binding, the exception middleware's
/// status codes — is covered by <see cref="ApiFixture"/> instead.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly TestDatabase _database = new();

    private ServiceProvider _services = null!;

    /// <summary>Throwaway directory backing file storage for this run.</summary>
    public string StorageRoot { get; } = Path.Combine(
        Path.GetTempPath(), "construction-tests", Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync()
    {
        await _database.CreateAsync();

        _services = BuildServiceProvider();

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();

        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }

        await _database.DropAsync();
    }

    /// <summary>
    /// A scope per logical request, mirroring how the API resolves handlers.
    /// </summary>
    public TestScope CreateScope() => new(_services.CreateScope());

    private ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _database.ConnectionString,
                ["JwtSettings:Issuer"] = "construction-api-tests",
                ["JwtSettings:Audience"] = "construction-clients-tests",
                ["JwtSettings:SecretKey"] = "integration-test-signing-key-at-least-32-chars",
                ["JwtSettings:AccessTokenLifetimeMinutes"] = "15",
                ["JwtSettings:RefreshTokenLifetimeDays"] = "7",
                // Left unconfigured on purpose: the email and push senders log
                // instead of reaching the network, so no test touches either.
                ["ClientApp:PasswordResetUrl"] = "https://admin.example.test/reset-password",
                // The real filesystem storage, pointed at a throwaway
                // directory. A fake would let an upload test pass while the
                // code that actually writes bytes went unexercised.
                ["FileStorage:RootPath"] = StorageRoot
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
