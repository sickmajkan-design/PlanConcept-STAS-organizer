using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class FuelCardConfiguration : IEntityTypeConfiguration<FuelCard>
{
    public void Configure(EntityTypeBuilder<FuelCard> builder)
    {
        builder.ToTable("fuel_cards");

        builder.HasKey(c => c.Id);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.Provider)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.CardNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(c => c.CardNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(c => c.Note)
            .HasMaxLength(500);

        builder.HasOne(c => c.Vehicle)
            .WithMany(v => v.FuelCards)
            .HasForeignKey(c => c.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.VehicleId);
    }
}
