using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class DocumentRequestConfiguration : IEntityTypeConfiguration<DocumentRequest>
{
    public void Configure(EntityTypeBuilder<DocumentRequest> builder)
    {
        builder.ToTable("document_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Note).HasMaxLength(500);
        builder.Property(r => r.ReturnReason).HasMaxLength(500);

        // Removing a person removes what was asked of them (an erasure request has to).
        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RequestedByUser)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.EmployeeId, r.Status });
        builder.HasIndex(r => new { r.Status, r.DueDate });

        builder.Property<uint>("Version").IsRowVersion();
    }
}

public class DocumentRequestEventConfiguration : IEntityTypeConfiguration<DocumentRequestEvent>
{
    public void Configure(EntityTypeBuilder<DocumentRequestEvent> builder)
    {
        builder.ToTable("document_request_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason).HasMaxLength(500);

        builder.Property(e => e.AttachmentIds)
            .HasColumnType("uuid[]")
            .HasDefaultValueSql("'{}'");

        builder.HasOne(e => e.DocumentRequest)
            .WithMany(r => r.Events)
            .HasForeignKey(e => e.DocumentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.DocumentRequestId);
    }
}
