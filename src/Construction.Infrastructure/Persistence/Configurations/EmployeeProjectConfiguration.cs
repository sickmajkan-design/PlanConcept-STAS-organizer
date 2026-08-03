using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EmployeeProjectConfiguration : IEntityTypeConfiguration<EmployeeProject>
{
    public void Configure(EntityTypeBuilder<EmployeeProject> builder)
    {
        builder.ToTable("employee_projects");

        // A surrogate key rather than (EmployeeId, ProjectId): the same person
        // returning to the same site in a later month is a second posting, and
        // the composite key made that unrepresentable.
        builder.HasKey(ep => ep.Id);

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

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_employee_projects_ends_after_start",
            "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));

        // "Who is on this site" and "where is this person", both by date.
        builder.HasIndex(ep => new { ep.EmployeeId, ep.StartDate });
        builder.HasIndex(ep => new { ep.ProjectId, ep.StartDate });
    }
}
