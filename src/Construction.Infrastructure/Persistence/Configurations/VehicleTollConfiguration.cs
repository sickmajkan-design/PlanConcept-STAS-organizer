using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleTollConfiguration : IEntityTypeConfiguration<VehicleToll>
{
    public void Configure(EntityTypeBuilder<VehicleToll> builder)
    {
        builder.ToTable("vehicle_tolls");

        builder.HasKey(t => t.Id);

        // Tolls for a deleted vehicle are not chargeable to anything.
        builder.HasQueryFilter(t => !t.Vehicle.IsDeleted);

        builder.Property(t => t.Country).IsRequired().HasMaxLength(100);
        builder.Property(t => t.RouteSegment).HasMaxLength(200);

        // Same reasoning as VehicleExpenseConfiguration's Status default:
        // the CLR default of an unset enum column is 0, which none of
        // VehicleTollStatus's members define.
        builder.Property(t => t.Status).HasDefaultValue(VehicleTollStatus.Unpaid);

        builder.HasOne(t => t.Vehicle)
            .WithMany(v => v.Tolls)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.PaidByUser)
            .WithMany()
            .HasForeignKey(t => t.PaidByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // "What is this vehicle carrying, soonest to expire first" — the
        // only way the vehicle detail screen and the reminder sweep read it.
        builder.HasIndex(t => new { t.VehicleId, t.ValidUntil });
    }
}
