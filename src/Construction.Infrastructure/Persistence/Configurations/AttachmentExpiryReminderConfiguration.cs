using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class AttachmentExpiryReminderConfiguration : IEntityTypeConfiguration<AttachmentExpiryReminder>
{
    public void Configure(EntityTypeBuilder<AttachmentExpiryReminder> builder)
    {
        builder.ToTable("attachment_expiry_reminders");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Attachment)
            .WithMany()
            .HasForeignKey(r => r.AttachmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The claim itself: one row per (document, admin) is what stops the
        // sweep from telling the same person about the same document twice.
        builder.HasIndex(r => new { r.AttachmentId, r.UserId }).IsUnique();
    }
}
