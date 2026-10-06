using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleDateReminderConfiguration : IEntityTypeConfiguration<VehicleDateReminder>
{
    public void Configure(EntityTypeBuilder<VehicleDateReminder> builder)
    {
        builder.ToTable("vehicle_date_reminders");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // The claim: one row per (vehicle, date kind, the date itself, stage). A second sweep that
        // meets it fails the insert and stays quiet instead of racing a check-then-set.
        builder.HasIndex(r => new { r.VehicleId, r.Kind, r.DueDate, r.DaysBefore }).IsUnique();
    }
}
