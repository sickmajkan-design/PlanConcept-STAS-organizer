using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleFineConfiguration : IEntityTypeConfiguration<VehicleFine>
{
    public void Configure(EntityTypeBuilder<VehicleFine> builder)
    {
        builder.ToTable("vehicle_fines");

        builder.HasKey(f => f.Id);

        builder.HasQueryFilter(f => !f.Vehicle.IsDeleted);

        builder.Property(f => f.Description).HasMaxLength(300).IsRequired();
        builder.Property(f => f.Amount).HasPrecision(18, 2);
        builder.Property(f => f.Note).HasMaxLength(500);

        builder.HasOne(f => f.Vehicle)
            .WithMany(v => v.Fines)
            .HasForeignKey(f => f.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.DriverEmployee)
            .WithMany()
            .HasForeignKey(f => f.DriverEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(f => f.RecordedByUser)
            .WithMany()
            .HasForeignKey(f => f.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint("ck_vehicle_fines_amount_not_negative", "\"Amount\" >= 0"));

        builder.HasIndex(f => new { f.VehicleId, f.OccurredOn });
        // What is still open, by when: the daily sweep and the vehicles list read it.
        builder.HasIndex(f => new { f.Status, f.DueDate });
        // What a driver owes.
        builder.HasIndex(f => new { f.DriverEmployeeId, f.Status });
    }
}

public class VehicleFineReminderConfiguration : IEntityTypeConfiguration<VehicleFineReminder>
{
    public void Configure(EntityTypeBuilder<VehicleFineReminder> builder)
    {
        builder.ToTable("vehicle_fine_reminders");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.VehicleFine)
            .WithMany()
            .HasForeignKey(r => r.VehicleFineId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per (fine, stage, due date): a moved due date is announced afresh.
        builder.HasIndex(r => new { r.VehicleFineId, r.DaysBefore, r.DueDate }).IsUnique();
    }
}
