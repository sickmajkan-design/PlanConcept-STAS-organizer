using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LeaveAdjustmentConfiguration : IEntityTypeConfiguration<LeaveAdjustment>
{
    public void Configure(EntityTypeBuilder<LeaveAdjustment> builder)
    {
        builder.ToTable("leave_adjustments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Reason).IsRequired().HasMaxLength(500);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint("ck_leave_adjustments_days_not_zero", "\"Days\" <> 0"));

        builder.HasIndex(a => new { a.EmployeeId, a.Year });
    }
}
