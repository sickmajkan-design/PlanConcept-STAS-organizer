using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class WeeklyReportReminderConfiguration : IEntityTypeConfiguration<WeeklyReportReminder>
{
    public void Configure(EntityTypeBuilder<WeeklyReportReminder> builder)
    {
        builder.ToTable("weekly_report_reminders");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // The claim itself: one row per (site, employee, week) is what stops
        // the sweep from reminding the same person about the same week twice.
        builder.HasIndex(r => new { r.ProjectId, r.EmployeeId, r.IsoYear, r.IsoWeek }).IsUnique();
    }
}
