using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class CompanyRevenueConfiguration : IEntityTypeConfiguration<CompanyRevenue>
{
    public void Configure(EntityTypeBuilder<CompanyRevenue> builder)
    {
        builder.ToTable("company_revenues");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.Property(r => r.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Tool)
            .WithMany()
            .HasForeignKey(r => r.ToolId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.RecordedByUser)
            .WithMany()
            .HasForeignKey(r => r.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_company_revenues_amount_positive", "\"Amount\" > 0"));

        builder.HasIndex(r => r.OccurredOn);
        builder.HasIndex(r => r.VehicleId);
        builder.HasIndex(r => r.ToolId);
    }
}
