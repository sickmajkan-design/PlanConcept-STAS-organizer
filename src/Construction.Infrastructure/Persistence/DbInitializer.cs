using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Construction.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds the initial Super Admin account.
/// Called once on API startup (gated by Database:ApplyMigrationsOnStartup).
/// </summary>
public class DbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DbInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count > 0)
        {
            _logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count, string.Join(", ", pendingMigrations));

            await _context.Database.MigrateAsync(cancellationToken);
        }

        await SeedSuperAdminAsync(cancellationToken);
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        var superAdminExists = await _context.Users
            .AnyAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);

        if (superAdminExists)
        {
            return;
        }

        var email = _configuration["Seed:SuperAdmin:Email"];
        var password = _configuration["Seed:SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "No Super Admin account exists and Seed:SuperAdmin:Email / Seed:SuperAdmin:Password " +
                "are not configured. Skipping seeding — the API has no usable account until these are set.");
            return;
        }

        _context.Users.Add(new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.SuperAdmin,
            IsActive = true
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded initial Super Admin account {Email}", email);
    }
}
