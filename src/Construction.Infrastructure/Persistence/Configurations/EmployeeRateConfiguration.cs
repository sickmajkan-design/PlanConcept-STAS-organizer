using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EmployeeRateConfiguration : IEntityTypeConfiguration<EmployeeRate>
{
    public void Configure(EntityTypeBuilder<EmployeeRate> builder)
    {
        builder.ToTable("employee_rates");

        builder.HasKey(r => r.Id);

        // Rates for a deleted employee are not chargeable to anything.
        builder.HasQueryFilter(r => !r.Employee.IsDeleted);

        // Money, not a measurement: two decimals and no binary floating point
        // anywhere near it.
        builder.Property(r => r.HourlyRate).HasPrecision(18, 2);

        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Employee)
            .WithMany(e => e.Rates)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SetByUser)
            .WithMany()
            .HasForeignKey(r => r.SetByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_employee_rates_ends_after_start",
                "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            // A free hour is a data-entry slip, and it costs a project money
            // silently rather than loudly.
            t.HasCheckConstraint(
                "ck_employee_rates_positive", "\"HourlyRate\" > 0");
        });

        // "What did this person cost per hour on day D" — the join every cost
        // report makes, once per employee.
        builder.HasIndex(r => new { r.EmployeeId, r.StartDate });
    }
}
