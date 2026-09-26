using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount).HasPrecision(12, 2);
        builder.Property(r => r.Currency).IsRequired().HasMaxLength(3);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.ReviewNote).HasMaxLength(1000);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequestedByUser)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint("ck_refunds_amount_positive", "\"Amount\" > 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_refunds_payroll_month", "\"PayrollMonth\" IS NULL OR \"PayrollMonth\" BETWEEN 1 AND 12"));

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.EmployeeId, r.PayrollYear, r.PayrollMonth });

        builder.Property<uint>("Version").IsRowVersion();
    }
}
