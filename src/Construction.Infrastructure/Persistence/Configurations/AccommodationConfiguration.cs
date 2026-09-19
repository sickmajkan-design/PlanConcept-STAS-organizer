using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class AccommodationConfiguration : IEntityTypeConfiguration<Accommodation>
{
    public void Configure(EntityTypeBuilder<Accommodation> builder)
    {
        builder.ToTable("accommodations");

        builder.HasKey(a => a.Id);

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Property(a => a.Address)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(a => a.Name).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(120);
        builder.Property(a => a.Floor).HasMaxLength(40);
        builder.Property(a => a.AreaSquareMeters).HasPrecision(8, 2);
        builder.Property(a => a.LandlordName).HasMaxLength(200);
        builder.Property(a => a.LandlordPhone).HasMaxLength(60);
        builder.Property(a => a.LandlordEmail).HasMaxLength(200);
        builder.Property(a => a.ContractNumber).HasMaxLength(100);
        builder.Property(a => a.DepositAmount).HasPrecision(18, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_accommodations_beds_positive", "\"Beds\" IS NULL OR \"Beds\" > 0");
            t.HasCheckConstraint("ck_accommodations_rooms_positive", "\"Rooms\" IS NULL OR \"Rooms\" > 0");
            t.HasCheckConstraint(
                "ck_accommodations_contract_ends_after_start",
                "\"ContractEnd\" IS NULL OR \"ContractStart\" IS NULL OR \"ContractEnd\" >= \"ContractStart\"");
        });

        builder.Property(a => a.Note)
            .HasMaxLength(2000);

        builder.HasIndex(a => a.Address);
    }
}
