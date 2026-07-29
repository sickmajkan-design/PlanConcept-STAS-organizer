using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.ToTable("tools");

        builder.HasKey(t => t.Id);

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.Property(t => t.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.Category)
            .HasMaxLength(128);

        builder.Property(t => t.SerialNumber)
            .HasMaxLength(128);

        builder.HasIndex(t => t.SerialNumber)
            .IsUnique()
            .HasFilter("\"SerialNumber\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.Property(t => t.QrCode)
            .HasMaxLength(256);

        builder.HasIndex(t => t.QrCode)
            .IsUnique()
            .HasFilter("\"QrCode\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasOne(t => t.AssignedEmployee)
            .WithMany(e => e.AssignedTools)
            .HasForeignKey(t => t.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.AssignedProject)
            .WithMany(p => p.AssignedTools)
            .HasForeignKey(t => t.AssignedProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.AssignedEmployeeId);
        builder.HasIndex(t => t.AssignedProjectId);
        builder.HasIndex(t => t.Status);
    }
}
