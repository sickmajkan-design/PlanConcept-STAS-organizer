using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class DashboardLayoutConfiguration : IEntityTypeConfiguration<DashboardLayout>
{
    public void Configure(EntityTypeBuilder<DashboardLayout> builder)
    {
        builder.ToTable("dashboard_layouts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.WidgetsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasOne(d => d.User)
            .WithOne()
            .HasForeignKey<DashboardLayout>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.UserId).IsUnique();
    }
}
