using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EmployeeProjectConfiguration : IEntityTypeConfiguration<EmployeeProject>
{
    public void Configure(EntityTypeBuilder<EmployeeProject> builder)
    {
        builder.ToTable("employee_projects");

        builder.HasKey(ep => new { ep.EmployeeId, ep.ProjectId });

        // Keep assignments out of query results when either side is soft-deleted.
        builder.HasQueryFilter(ep => !ep.Employee.IsDeleted && !ep.Project.IsDeleted);

        builder.HasOne(ep => ep.Employee)
            .WithMany(e => e.ProjectAssignments)
            .HasForeignKey(ep => ep.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ep => ep.Project)
            .WithMany(p => p.EmployeeAssignments)
            .HasForeignKey(ep => ep.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ep => ep.ProjectId);
    }
}
