using System.Net.Http.Headers;
using System.Net.Http.Json;
using Construction.API.BackgroundServices;
using Construction.Application.Features.Authentication.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Construction.Infrastructure.Authentication;
using Construction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Construction.IntegrationTests;

/// <summary>
/// Hosts the real API in-process, over its own throwaway database, with one
/// signed-in account per role.
/// </summary>
/// <remarks>
/// <para>
/// The other fixture sends commands through MediatR with the current user set
/// directly, which is the right shape for testing what a handler does — but it
/// walks straight past everything that decides whether the handler should have
/// run at all. A controller action missing its <c>[Authorize]</c> attribute, a
/// policy naming the wrong roles, a route left anonymous: all of that ships
/// green in a suite that never issues a request.
/// </para>
/// <para>
/// So this one goes over HTTP with a real bearer token, minted by the real
/// login endpoint from a real password hash. Slower, and the only way the
/// authorization model is anything but a claim.
/// </para>
/// </remarks>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly TestDatabase _database = new();

    private readonly Dictionary<UserRole, string> _tokens = [];

    private WebApplicationFactory<Program> _factory = null!;

    /// <summary>Throwaway directory backing file storage for this run.</summary>
    public string StorageRoot { get; } = Path.Combine(
        Path.GetTempPath(), "construction-api-tests", Guid.NewGuid().ToString("N"));

    /// <summary>The seeded account for each role, so a test can name its own data.</summary>
    public Dictionary<UserRole, Guid> UserIds { get; } = [];

    public async Task InitializeAsync()
    {
        await _database.CreateAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Development skips the production configuration check, which
                // would otherwise demand SMTP and a public reset URL.
                builder.UseEnvironment("Development");

                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection", _database.ConnectionString);
                builder.UseSetting("JwtSettings:Issuer", "construction-api-tests");
                builder.UseSetting("JwtSettings:Audience", "construction-clients-tests");
                builder.UseSetting(
                    "JwtSettings:SecretKey", "integration-test-signing-key-at-least-32-chars");
                builder.UseSetting("JwtSettings:AccessTokenLifetimeMinutes", "15");
                builder.UseSetting("JwtSettings:RefreshTokenLifetimeDays", "7");
                builder.UseSetting("FileStorage:RootPath", StorageRoot);

                // Migrations are applied below, once, rather than by the app on
                // every start.
                builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");

                builder.ConfigureServices(services =>
                {
                    // The product's own timers, not every hosted service: the
                    // web host itself is registered as one, and removing the
                    // lot would leave nothing listening. A reminder sweep
                    // firing mid-test would write rows no test asked for, and
                    // a retention sweep would delete rows one was using.
                    foreach (var timer in services
                        .Where(descriptor =>
                            descriptor.ImplementationType == typeof(DailyReminderService) ||
                            descriptor.ImplementationType == typeof(DataRetentionService))
                        .ToList())
                    {
                        services.Remove(timer);
                    }

                    services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
                });
            });

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
        }

        await SeedAccountsAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();

        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }

        await _database.DropAsync();
    }

    /// <summary>A client carrying no credentials at all.</summary>
    public HttpClient AnonymousClient() => _factory.CreateClient();

    /// <summary>A client signed in as the seeded account for <paramref name="role"/>.</summary>
    public HttpClient ClientAs(UserRole role)
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _tokens[role]);

        return client;
    }

    /// <summary>Runs work against the hosted app's own services, for seeding and assertions.</summary>
    public async Task<T> InScope<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    /// <summary>One account per role, signed in and ready.</summary>
    private async Task SeedAccountsAsync()
    {
        foreach (var role in Enum.GetValues<UserRole>())
        {
            var (email, userId) = await SeedSignInAccountAsync(role, $"{role}@api-tests.test");

            UserIds[role] = userId;
            _tokens[role] = await SignInAsync(email);
        }
    }

    /// <summary>
    /// One account that can actually sign in, with an employee behind it.
    /// </summary>
    /// <remarks>
    /// The employee link is deliberate: several endpoints answer differently
    /// for an account with nobody behind it, and an unlinked account would
    /// make a role look forbidden when it was only unlinked — a false pass,
    /// since the test would still see the refusal it expected.
    /// </remarks>
    public async Task<(string Email, Guid UserId)> SeedSignInAccountAsync(
        UserRole role,
        string? email = null)
    {
        var address = (email ?? $"{role}-{Guid.NewGuid():N}@api-tests.test").ToLowerInvariant();

        var userId = await InScope(async context =>
        {
            var employee = new Employee
            {
                EmployeeNumber = $"API-{Guid.NewGuid():N}"[..20],
                FirstName = role.ToString(),
                LastName = "Tester",
                Position = "Tester",
                EmploymentDate = new DateOnly(2020, 1, 1),
                Status = EmployeeStatus.Active
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            var user = new User
            {
                Email = address,
                PasswordHash = new PasswordHasher().Hash(TestData.Password),
                Role = role,
                IsActive = true,
                EmployeeId = employee.Id
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user.Id;
        });

        return (address, userId);
    }

    /// <summary>
    /// Signs in over HTTP rather than minting a token from the signing service.
    /// </summary>
    /// <remarks>
    /// The claims a token carries are part of what is under test: a role claim
    /// written under the wrong claim type would satisfy a hand-built token and
    /// fail every real login. Going through the endpoint means the token these
    /// tests carry is the token a client gets.
    ///
    /// Sign-in sits behind the credentials rate limiter (twenty a minute, and
    /// every caller shares one partition because a test server has no remote
    /// address). One login per role at start-up leaves plenty of room, but a
    /// test that logs in per case would spend the window and start reading
    /// 429 as a refusal.
    /// </remarks>
    public async Task<string> SignInAsync(string email)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = TestData.Password });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException($"No token came back for {email}.");

        return auth.AccessToken;
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
