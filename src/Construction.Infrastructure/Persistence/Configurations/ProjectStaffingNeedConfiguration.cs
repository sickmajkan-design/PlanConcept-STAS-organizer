using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ProjectStaffingNeedConfiguration : IEntityTypeConfiguration<ProjectStaffingNeed>
{
    public void Configure(EntityTypeBuilder<ProjectStaffingNeed> builder)
    {
        builder.ToTable("project_staffing_needs");

        builder.HasKey(n => n.Id);

        builder.HasQueryFilter(n => !n.Project.IsDeleted);

        builder.Property(n => n.Position).IsRequired().HasMaxLength(100);

        builder.HasOne(n => n.Project)
            .WithMany(p => p.StaffingNeeds)
            .HasForeignKey(n => n.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.ProjectId);

        builder.ToTable(t => t.HasCheckConstraint("ck_project_staffing_needs_count", "\"Count\" > 0"));
    }
}
