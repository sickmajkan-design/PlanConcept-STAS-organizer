using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleRentalRateConfiguration : IEntityTypeConfiguration<VehicleRentalRate>
{
    public void Configure(EntityTypeBuilder<VehicleRentalRate> builder)
    {
        builder.ToTable("vehicle_rental_rates");

        builder.HasKey(r => r.Id);

        // Rates for a deleted vehicle are not chargeable to anything.
        builder.HasQueryFilter(r => !r.Vehicle.IsDeleted);

        builder.Property(r => r.MonthlyAmount).HasPrecision(18, 2);

        builder.Property(r => r.Provider).HasMaxLength(200);

        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Vehicle)
            .WithMany(v => v.RentalRates)
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SetByUser)
            .WithMany()
            .HasForeignKey(r => r.SetByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_vehicle_rental_rates_ends_after_start",
                "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            t.HasCheckConstraint(
                "ck_vehicle_rental_rates_positive", "\"MonthlyAmount\" > 0");
        });

        // "What is this vehicle costing us on day D" — the same join
        // EmployeeRate's equivalent index describes, for the fleet instead
        // of the crew.
        builder.HasIndex(r => new { r.VehicleId, r.StartDate });
    }
}
