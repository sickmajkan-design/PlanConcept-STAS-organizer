using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<EmployeeProject> EmployeeProjects => Set<EmployeeProject>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Tool> Tools => Set<Tool>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<LocationRecord> LocationRecords => Set<LocationRecord>();

    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    public DbSet<Absence> Absences => Set<Absence>();

    public DbSet<EmployeeRate> EmployeeRates => Set<EmployeeRate>();

    public DbSet<MaterialMovement> MaterialMovements => Set<MaterialMovement>();

    public DbSet<VehicleExpense> VehicleExpenses => Set<VehicleExpense>();

    public DbSet<FinanceEntry> FinanceEntries => Set<FinanceEntry>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        // Joining an open transaction rather than nesting: PostgreSQL has no
        // nested transactions, and starting a second one here would throw
        // rather than do what the caller means. The outermost caller owns the
        // commit either way.
        if (Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            return;
        }

        // The execution strategy owns the retry loop, so the whole block is
        // replayed on a transient failure instead of half of it.
        var strategy = Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await Database.BeginTransactionAsync(cancellationToken);

            await action(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
