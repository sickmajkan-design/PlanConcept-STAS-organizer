using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleRentalOutConfiguration : IEntityTypeConfiguration<VehicleRentalOut>
{
    public void Configure(EntityTypeBuilder<VehicleRentalOut> builder)
    {
        builder.ToTable("vehicle_rentals_out");

        builder.HasKey(r => r.Id);

        builder.HasQueryFilter(r => !r.Vehicle.IsDeleted);

        builder.Property(r => r.DailyRate).HasPrecision(18, 2);

        builder.Property(r => r.RenterName).HasMaxLength(200).IsRequired();

        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Vehicle)
            .WithMany(v => v.RentalsOut)
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.SetByUser)
            .WithMany()
            .HasForeignKey(r => r.SetByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_vehicle_rentals_out_ends_after_start",
                "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            t.HasCheckConstraint(
                "ck_vehicle_rentals_out_rate_positive", "\"DailyRate\" > 0");
        });

        // At most one open loan per vehicle — the same vehicle cannot be out
        // with two different companies at once.
        builder.HasIndex(r => r.VehicleId)
            .IsUnique()
            .HasDatabaseName("ix_vehicle_rentals_out_one_open_per_vehicle")
            .HasFilter("\"EndDate\" IS NULL");

        builder.HasIndex(r => new { r.VehicleId, r.StartDate });
        builder.HasIndex(r => r.CustomerId);
    }
}
