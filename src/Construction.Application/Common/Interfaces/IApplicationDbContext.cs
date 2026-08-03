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

    DbSet<TimeEntry> TimeEntries { get; }

    DbSet<Attachment> Attachments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
