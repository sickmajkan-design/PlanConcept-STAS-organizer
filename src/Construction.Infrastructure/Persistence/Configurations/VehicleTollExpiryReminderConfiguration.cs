using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleTollExpiryReminderConfiguration : IEntityTypeConfiguration<VehicleTollExpiryReminder>
{
    public void Configure(EntityTypeBuilder<VehicleTollExpiryReminder> builder)
    {
        builder.ToTable("vehicle_toll_expiry_reminders");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.VehicleToll)
            .WithMany()
            .HasForeignKey(r => r.VehicleTollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The claim itself: one row per (toll, admin, validity period) is
        // what stops the sweep from telling the same person about the same
        // renewal twice, while still notifying again after a fresh renewal.
        builder.HasIndex(r => new { r.VehicleTollId, r.UserId, r.ValidUntil }).IsUnique();
    }
}
