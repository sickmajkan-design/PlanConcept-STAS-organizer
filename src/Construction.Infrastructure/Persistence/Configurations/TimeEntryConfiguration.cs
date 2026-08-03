using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("time_entries");

        builder.HasKey(t => t.Id);

        builder.HasQueryFilter(t => !t.IsDeleted);

        // Computed from StartedAt, EndedAt and BreakMinutes; nothing to store.
        builder.Ignore(t => t.WorkedMinutes);
        builder.Ignore(t => t.IsLocked);

        builder.Property(t => t.Note)
            .HasMaxLength(1000);

        builder.Property(t => t.ReviewNote)
            .HasMaxLength(1000);

        builder.HasOne(t => t.Employee)
            .WithMany(e => e.TimeEntries)
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Project)
            .WithMany(p => p.TimeEntries)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Reviewer accounts are never hard-deleted, but if one ever were, the
        // hours must survive losing the name of who signed them off.
        builder.HasOne(t => t.ReviewedByUser)
            .WithMany()
            .HasForeignKey(t => t.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // A worker can only be on one shift at a time. Enforced here rather
        // than only in the handler because two clock-in requests racing each
        // other would both pass a handler check and both insert.
        builder.HasIndex(t => t.EmployeeId)
            .IsUnique()
            .HasDatabaseName("ix_time_entries_one_open_per_employee")
            .HasFilter("\"EndedAt\" IS NULL AND \"IsDeleted\" = false");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_time_entries_break_non_negative", "\"BreakMinutes\" >= 0"));

        // A shift cannot end before it starts. Cheap here, and it makes any
        // duration read out of the database safe to trust.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_time_entries_ends_after_start",
            "\"EndedAt\" IS NULL OR \"EndedAt\" > \"StartedAt\""));

        // The two shapes every screen asks for: one employee's entries in a
        // date range, and the review queue ordered by when work happened.
        builder.HasIndex(t => new { t.EmployeeId, t.StartedAt })
            .IsDescending(false, true);

        builder.HasIndex(t => new { t.Status, t.StartedAt })
            .IsDescending(false, true);

        builder.HasIndex(t => t.ProjectId);
    }
}
