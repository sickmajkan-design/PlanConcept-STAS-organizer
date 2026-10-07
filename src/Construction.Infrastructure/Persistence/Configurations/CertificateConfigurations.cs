using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EmployeeCertificateConfiguration : IEntityTypeConfiguration<EmployeeCertificate>
{
    public void Configure(EntityTypeBuilder<EmployeeCertificate> builder)
    {
        builder.ToTable("employee_certificates");

        builder.HasKey(c => c.Id);

        builder.HasQueryFilter(c => !c.Employee.IsDeleted);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Note).HasMaxLength(200);

        builder.HasOne(c => c.Employee)
            .WithMany()
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.EmployeeId);

        // "Which certificates run out soon" is asked across everybody.
        builder.HasIndex(c => c.ValidUntil);
    }
}

public class ProjectCertificateRequirementConfiguration : IEntityTypeConfiguration<ProjectCertificateRequirement>
{
    public void Configure(EntityTypeBuilder<ProjectCertificateRequirement> builder)
    {
        builder.ToTable("project_certificate_requirements");

        builder.HasKey(r => r.Id);

        builder.HasQueryFilter(r => !r.Project.IsDeleted);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(r => r.Project)
            .WithMany(p => p.CertificateRequirements)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.ProjectId);
    }
}
