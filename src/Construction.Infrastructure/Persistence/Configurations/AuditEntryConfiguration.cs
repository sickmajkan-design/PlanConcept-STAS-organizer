using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(a => a.EntityName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.UserEmail)
            .HasMaxLength(256);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(64);

        builder.Property(a => a.ChangesJson)
            .HasColumnType("jsonb")
            .IsRequired();

        // No foreign key to users on purpose. The trail has to outlive the
        // account — an account removed during an investigation would otherwise
        // take its own history with it, or block its own deletion.

        // The trail's one real query: "what happened to this record", newest
        // first. Composite because entity name alone is not selective — most
        // rows in a workforce system are employees.
        builder.HasIndex(a => new { a.EntityName, a.EntityId, a.OccurredAt })
            .IsDescending(false, false, true);

        // "What did this person do", for an investigation that starts from a
        // user rather than from a record.
        builder.HasIndex(a => new { a.UserId, a.OccurredAt })
            .IsDescending(false, true);

        // Time alone, for the unfiltered feed and for retention sweeps.
        builder.HasIndex(a => a.OccurredAt);
    }
}
