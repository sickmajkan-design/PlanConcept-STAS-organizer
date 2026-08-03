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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
