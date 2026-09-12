using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(4000);

        builder.Property(p => p.Address)
            .HasMaxLength(512);

        builder.Property(p => p.CountryCode)
            .HasMaxLength(2);

        builder.Property(p => p.ContractValue)
            .HasPrecision(18, 2);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_projects_contract_value_not_negative",
            "\"ContractValue\" IS NULL OR \"ContractValue\" >= 0"));

        builder.HasOne(p => p.Customer)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ParentProject)
            .WithMany(p => p.SubProjects)
            .HasForeignKey(p => p.ParentProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.ParentProjectId);
    }
}
