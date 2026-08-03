using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");

        builder.HasKey(a => a.Id);

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Property(a => a.FileName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        // One row per stored object. Without this a bug that reused a key
        // would leave two rows pointing at the same bytes, and deleting either
        // would break the other.
        builder.HasIndex(a => a.StorageKey).IsUnique();

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Attachments)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Attachments)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Vehicle)
            .WithMany(v => v.Attachments)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Tool)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.WorkItem)
            .WithMany(w => w.Attachments)
            .HasForeignKey(a => a.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Uploader accounts are not hard-deleted, but the file must survive
        // losing the name of who put it there.
        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Exactly one owner. A file belonging to nothing is unreachable and
        // silently orphans its bytes; one belonging to two would appear on two
        // screens and be deleted from one of them.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_attachments_exactly_one_owner",
            """
            (CASE WHEN "EmployeeId" IS NULL THEN 0 ELSE 1 END
            + CASE WHEN "ProjectId" IS NULL THEN 0 ELSE 1 END
            + CASE WHEN "VehicleId" IS NULL THEN 0 ELSE 1 END
            + CASE WHEN "ToolId" IS NULL THEN 0 ELSE 1 END
            + CASE WHEN "WorkItemId" IS NULL THEN 0 ELSE 1 END) = 1
            """));

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_attachments_size_positive", "\"SizeBytes\" > 0"));

        // "This record's documents", the only way the lists are read.
        builder.HasIndex(a => a.EmployeeId);
        builder.HasIndex(a => a.ProjectId);
        builder.HasIndex(a => a.VehicleId);
        builder.HasIndex(a => a.ToolId);
        builder.HasIndex(a => a.WorkItemId);

        // The expiry sweep: everything lapsing soon that nobody has been told
        // about. Partial, because rows without an expiry are most of the table
        // and never match.
        builder.HasIndex(a => a.ExpiresAt)
            .HasDatabaseName("ix_attachments_pending_expiry")
            .HasFilter("\"ExpiresAt\" IS NOT NULL AND \"ExpiryReminderSentAt\" IS NULL AND \"IsDeleted\" = false");
    }
}
