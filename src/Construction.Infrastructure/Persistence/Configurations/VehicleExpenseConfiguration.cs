using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleExpenseConfiguration : IEntityTypeConfiguration<VehicleExpense>
{
    public void Configure(EntityTypeBuilder<VehicleExpense> builder)
    {
        builder.ToTable("vehicle_expenses");

        builder.HasKey(e => e.Id);

        builder.HasQueryFilter(e => !e.Vehicle.IsDeleted);

        builder.Ignore(e => e.PricePerLitre);

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Litres).HasPrecision(18, 3);

        builder.Property(e => e.Supplier).HasMaxLength(200);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasOne(e => e.Vehicle)
            .WithMany(v => v.Expenses)
            .HasForeignKey(e => e.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_vehicle_expenses_amount_not_negative", "\"Amount\" >= 0");

            // Litres belong to a fill-up and nowhere else. Without this the
            // one field that distinguishes the kinds can be set on any of
            // them, and the fuel report starts counting insurance premiums.
            // 1 is VehicleExpenseKind.Fuel.
            //
            // Written as a CASE rather than the obvious
            //   (Kind = 1 AND Litres > 0) OR (Kind <> 1 AND Litres IS NULL)
            // because that version lets a fill-up through with no litres at
            // all: `Litres > 0` is NULL when Litres is, `TRUE AND NULL` is
            // NULL, and a CHECK only rejects on FALSE. Every branch here
            // yields a real boolean.
            t.HasCheckConstraint(
                "ck_vehicle_expenses_litres_only_for_fuel",
                """
                CASE WHEN "Kind" = 1
                     THEN "Litres" IS NOT NULL AND "Litres" > 0
                     ELSE "Litres" IS NULL
                END
                """);

            t.HasCheckConstraint(
                "ck_vehicle_expenses_odometer_not_negative",
                "\"OdometerKm\" IS NULL OR \"OdometerKm\" >= 0");
        });

        // "What has this van cost, over this period" — the only way the
        // report reads it.
        builder.HasIndex(e => new { e.VehicleId, e.OccurredOn });
        builder.HasIndex(e => e.Kind);
    }
}
