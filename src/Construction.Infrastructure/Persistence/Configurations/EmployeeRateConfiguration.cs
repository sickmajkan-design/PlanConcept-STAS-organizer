using Construction.Domain.Entities;
using Construction.Domain.Enums;
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
        builder.Property(r => r.WeekendHourlyRate).HasPrecision(18, 2);
        builder.Property(r => r.HolidayHourlyRate).HasPrecision(18, 2);
        builder.Property(r => r.OvertimeHourlyRate).HasPrecision(18, 2);
        builder.Property(r => r.TravelHourlyRate).HasPrecision(18, 2);
        builder.Property(r => r.DailyRate).HasPrecision(18, 2);

        builder.Property(r => r.Note).HasMaxLength(500);

        builder.Property(r => r.RateType)
            .HasDefaultValue(RateType.Hourly);

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

            // A free hour (or day) is a data-entry slip, and it costs a
            // project money silently rather than loudly.
            t.HasCheckConstraint(
                "ck_employee_rates_positive",
                "\"HourlyRate\" IS NULL OR \"HourlyRate\" > 0");

            t.HasCheckConstraint(
                "ck_employee_rates_weekend_positive",
                "\"WeekendHourlyRate\" IS NULL OR \"WeekendHourlyRate\" > 0");

            t.HasCheckConstraint(
                "ck_employee_rates_holiday_positive",
                "\"HolidayHourlyRate\" IS NULL OR \"HolidayHourlyRate\" > 0");

            t.HasCheckConstraint(
                "ck_employee_rates_overtime_positive",
                "\"OvertimeHourlyRate\" IS NULL OR \"OvertimeHourlyRate\" > 0");

            t.HasCheckConstraint(
                "ck_employee_rates_travel_positive",
                "\"TravelHourlyRate\" IS NULL OR \"TravelHourlyRate\" > 0");

            t.HasCheckConstraint(
                "ck_employee_rates_daily_positive",
                "\"DailyRate\" IS NULL OR \"DailyRate\" > 0");

            // RateType picks which shape this row is — 1 (Hourly) carries an
            // HourlyRate and no DailyRate, 2 (Daily) the reverse. Written as
            // the raw enum values rather than a name because a check
            // constraint cannot reference the C# enum; the application layer
            // is what keeps these two things meaning the same thing.
            t.HasCheckConstraint(
                "ck_employee_rates_type_matches_fields",
                "(\"RateType\" = 1 AND \"HourlyRate\" IS NOT NULL AND \"DailyRate\" IS NULL) OR "
                    + "(\"RateType\" = 2 AND \"DailyRate\" IS NOT NULL AND \"HourlyRate\" IS NULL)");
        });

        // "What did this person cost per hour on day D" — the join every cost
        // report makes, once per employee.
        builder.HasIndex(r => new { r.EmployeeId, r.StartDate });
    }
}
