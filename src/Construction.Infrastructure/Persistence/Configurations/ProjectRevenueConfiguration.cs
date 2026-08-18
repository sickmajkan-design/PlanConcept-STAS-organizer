using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ProjectRevenueConfiguration : IEntityTypeConfiguration<ProjectRevenue>
{
    public void Configure(EntityTypeBuilder<ProjectRevenue> builder)
    {
        builder.ToTable("project_revenues");

        builder.HasKey(r => r.Id);

        builder.HasQueryFilter(r => !r.Project.IsDeleted);

        builder.Property(r => r.Amount).HasPrecision(18, 2);

        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Project)
            .WithMany(p => p.Revenues)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RecordedByUser)
            .WithMany()
            .HasForeignKey(r => r.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_project_revenues_amount_not_negative", "\"Amount\" >= 0"));

        // "What has come in against this site, and when" — the two directions
        // the realization plan reads it from.
        builder.HasIndex(r => new { r.ProjectId, r.OccurredOn });
    }
}
