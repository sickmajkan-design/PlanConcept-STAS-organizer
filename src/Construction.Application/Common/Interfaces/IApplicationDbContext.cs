using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    DbSet<Employee> Employees { get; }

    DbSet<Project> Projects { get; }

    DbSet<EmployeeProject> EmployeeProjects { get; }

    DbSet<Vehicle> Vehicles { get; }

    DbSet<Tool> Tools { get; }

    DbSet<Material> Materials { get; }

    DbSet<LocationRecord> LocationRecords { get; }

    DbSet<DeviceToken> DeviceTokens { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<NotificationGroup> NotificationGroups { get; }

    DbSet<NotificationGroupMember> NotificationGroupMembers { get; }

    DbSet<TimeEntry> TimeEntries { get; }

    DbSet<Attachment> Attachments { get; }

    DbSet<WorkItem> WorkItems { get; }

    DbSet<Absence> Absences { get; }

    DbSet<EmployeeRate> EmployeeRates { get; }

    DbSet<MaterialMovement> MaterialMovements { get; }

    DbSet<VehicleExpense> VehicleExpenses { get; }

    DbSet<FinanceEntry> FinanceEntries { get; }

    DbSet<ProjectRevenue> ProjectRevenues { get; }

    DbSet<OutboxMessage> OutboxMessages { get; }

    DbSet<AuditEntry> AuditEntries { get; }

    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="action"/> so that everything it writes lands
    /// together or not at all.
    /// </summary>
    /// <remarks>
    /// Almost every handler needs no transaction: a single
    /// <see cref="SaveChangesAsync"/> is already atomic, and asking for one
    /// around it would be noise. This exists for the handful that mix a
    /// tracked insert with an <c>ExecuteUpdate</c> — those bypass the change
    /// tracker and go straight to the database, so without this they are two
    /// statements that can half-succeed.
    ///
    /// Nesting is safe: an <c>action</c> that runs when a transaction is
    /// already open joins it rather than starting a second one.
    /// </remarks>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
