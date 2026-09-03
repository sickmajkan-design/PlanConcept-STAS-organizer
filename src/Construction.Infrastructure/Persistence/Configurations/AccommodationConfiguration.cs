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

        builder.Property(a => a.Note)
            .HasMaxLength(2000);

        builder.HasIndex(a => a.Address);
    }
}
