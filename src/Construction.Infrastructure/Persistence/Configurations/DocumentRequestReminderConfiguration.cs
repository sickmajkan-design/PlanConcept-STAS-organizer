using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class DocumentRequestReminderConfiguration : IEntityTypeConfiguration<DocumentRequestReminder>
{
    public void Configure(EntityTypeBuilder<DocumentRequestReminder> builder)
    {
        builder.ToTable("document_request_reminders");

        builder.HasKey(r => r.Id);

        // Removing a request (or the person it was for) removes what was recorded about reminding them.
        builder.HasOne(r => r.DocumentRequest)
            .WithMany()
            .HasForeignKey(r => r.DocumentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.Kind).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(r => new { r.DocumentRequestId, r.Kind, r.DueDate }).IsUnique();
    }
}
