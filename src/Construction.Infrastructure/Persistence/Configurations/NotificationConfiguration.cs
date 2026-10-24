using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(n => n.DataJson)
            .HasColumnType("jsonb");

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(n => n.Via).HasMaxLength(300);

        builder.HasOne(n => n.Broadcast)
            .WithMany()
            .HasForeignKey(n => n.BroadcastId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.BroadcastId);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);
    }
}
