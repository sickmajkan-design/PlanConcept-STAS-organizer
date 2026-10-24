using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class WeeklySiteReportPageConfiguration : IEntityTypeConfiguration<WeeklySiteReportPage>
{
    public void Configure(EntityTypeBuilder<WeeklySiteReportPage> builder)
    {
        builder.ToTable("weekly_site_report_pages");

        builder.HasKey(p => p.Id);

        // Hidden with the report it belongs to, which is hidden with its site or its submitter.
        builder.HasQueryFilter(p => !p.Report.Project.IsDeleted && !p.Report.SubmittedByEmployee.IsDeleted);

        builder.Property(p => p.FileName).HasMaxLength(512).IsRequired();
        builder.Property(p => p.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(p => p.StorageKey).HasMaxLength(1000).IsRequired();

        builder.HasOne(p => p.Report)
            .WithMany(r => r.Pages)
            .HasForeignKey(p => p.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ReportId, p.SortOrder });
    }
}

public class WeeklySiteReportLineConfiguration : IEntityTypeConfiguration<WeeklySiteReportLine>
{
    public void Configure(EntityTypeBuilder<WeeklySiteReportLine> builder)
    {
        builder.ToTable("weekly_site_report_lines");

        builder.HasKey(l => l.Id);

        builder.HasQueryFilter(l => !l.Report.Project.IsDeleted && !l.Report.SubmittedByEmployee.IsDeleted);

        builder.Property(l => l.Hours).HasPrecision(10, 2);
        builder.Property(l => l.HoursSecondMonth).HasPrecision(10, 2);

        builder.HasOne(l => l.Report)
            .WithMany(r => r.Lines)
            .HasForeignKey(l => l.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.ReportId, l.SortOrder });
        builder.HasIndex(l => l.EmployeeId);
    }
}
