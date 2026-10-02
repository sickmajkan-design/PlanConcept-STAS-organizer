using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EmployeeBranchConfiguration : IEntityTypeConfiguration<EmployeeBranch>
{
    public void Configure(EntityTypeBuilder<EmployeeBranch> builder)
    {
        builder.ToTable("employee_branches");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.BranchPeriods)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EmployeeId, x.StartDate });
        builder.HasIndex(x => x.BranchId);

        // At most one open-ended period per employee: "where they are now" has one answer.
        builder.HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasFilter("\"EndDate\" IS NULL")
            .HasDatabaseName("ux_employee_branches_one_open_period");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_employee_branches_end_not_before_start",
            "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
    }
}
