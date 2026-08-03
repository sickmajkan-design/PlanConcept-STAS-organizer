using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("work_items");

        builder.HasKey(w => w.Id);

        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.Ignore(w => w.IsFinished);

        builder.Property(w => w.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(w => w.Description)
            .HasMaxLength(4000);

        builder.HasOne(w => w.Project)
            .WithMany(p => p.WorkItems)
            .HasForeignKey(w => w.ProjectId)
            // Not SetNull: a defect with no site is unreachable, and the check
            // constraint below would reject the row anyway.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.AssignedEmployee)
            .WithMany(e => e.WorkItems)
            .HasForeignKey(w => w.AssignedEmployeeId)
            // An item outlives whoever was on it; it goes back to unassigned.
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.CreatedByUser)
            .WithMany()
            .HasForeignKey(w => w.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.ResolvedByUser)
            .WithMany()
            .HasForeignKey(w => w.ResolvedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // A defect happens somewhere. Without this the "defects on this site"
        // list silently misses the ones nobody attached to a site.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_work_items_defect_has_project",
            $"\"Kind\" <> {(int)WorkItemKind.Defect} OR \"ProjectId\" IS NOT NULL"));

        // Half a position is not a position.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_work_items_position_complete",
            "(\"Latitude\" IS NULL) = (\"Longitude\" IS NULL)"));

        // "What is on my plate", the query the phone makes on every launch.
        builder.HasIndex(w => new { w.AssignedEmployeeId, w.Status });

        // "What is open on this site", the board in the office.
        builder.HasIndex(w => new { w.ProjectId, w.Status });

        // The deadline sweep: everything due soon that nobody has been told
        // about. Partial, because items without a date never match.
        builder.HasIndex(w => w.DueDate)
            .HasDatabaseName("ix_work_items_pending_due_reminder")
            .HasFilter(
                "\"DueDate\" IS NOT NULL AND \"DueReminderSentAt\" IS NULL AND \"IsDeleted\" = false");
    }
}
