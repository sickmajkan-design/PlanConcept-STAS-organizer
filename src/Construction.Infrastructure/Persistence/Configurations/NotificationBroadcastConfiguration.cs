using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class NotificationBroadcastConfiguration : IEntityTypeConfiguration<NotificationBroadcast>
{
    public void Configure(EntityTypeBuilder<NotificationBroadcast> builder)
    {
        builder.ToTable("notification_broadcasts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title).HasMaxLength(256).IsRequired();

        builder.Property(b => b.Body).HasMaxLength(4000).IsRequired();

        builder.Property(b => b.AudienceJson).HasColumnType("jsonb").IsRequired();

        builder.HasOne(b => b.SentByUser)
            .WithMany()
            .HasForeignKey(b => b.SentByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(b => b.CreatedAt);
    }
}
