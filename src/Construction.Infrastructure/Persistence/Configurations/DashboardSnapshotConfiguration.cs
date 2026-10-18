using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class DashboardSnapshotConfiguration : IEntityTypeConfiguration<DashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<DashboardSnapshot> builder)
    {
        builder.ToTable("dashboard_snapshots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).HasMaxLength(KeyLength).IsRequired();
        builder.Property(s => s.Value).HasPrecision(18, 2);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.Day, s.Key }).IsUnique();
    }

    private const int KeyLength = 120;
}
