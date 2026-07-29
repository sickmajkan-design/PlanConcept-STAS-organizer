using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");

        builder.HasKey(m => m.Id);

        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.Property(m => m.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Unit)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.Quantity)
            .HasPrecision(18, 3);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_materials_quantity_non_negative", "\"Quantity\" >= 0"));

        builder.Property(m => m.Warehouse)
            .HasMaxLength(256);

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Materials)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.Name);
        builder.HasIndex(m => m.ProjectId);
        builder.HasIndex(m => m.Warehouse);
    }
}
