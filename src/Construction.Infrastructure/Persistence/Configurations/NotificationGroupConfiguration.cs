using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class NotificationGroupConfiguration : IEntityTypeConfiguration<NotificationGroup>
{
    public void Configure(EntityTypeBuilder<NotificationGroup> builder)
    {
        builder.ToTable("notification_groups");

        builder.HasKey(g => g.Id);

        builder.HasQueryFilter(g => !g.IsDeleted);

        builder.Property(g => g.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(g => g.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
