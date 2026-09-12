using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class WeeklySiteReportConfiguration : IEntityTypeConfiguration<WeeklySiteReport>
{
    public void Configure(EntityTypeBuilder<WeeklySiteReport> builder)
    {
        builder.ToTable("weekly_site_reports");

        builder.HasKey(r => r.Id);

        // A report for a deleted site or a deleted submitter documents
        // nothing chargeable — same reasoning FinanceEntry's filter uses.
        builder.HasQueryFilter(r =>
            !r.Project.IsDeleted && !r.SubmittedByEmployee.IsDeleted);

        builder.Property(r => r.Quantity).HasPrecision(10, 2);

        builder.Property(r => r.Note).HasMaxLength(1000);

        builder.Property(r => r.FileName).HasMaxLength(512).IsRequired();

        builder.Property(r => r.ContentType).HasMaxLength(200).IsRequired();

        builder.Property(r => r.StorageKey).HasMaxLength(1000).IsRequired();

        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SubmittedByEmployee)
            .WithMany()
            .HasForeignKey(r => r.SubmittedByEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ProcessedByUser)
            .WithMany()
            .HasForeignKey(r => r.ProcessedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // The only two ways this list is ever read: everything for one site,
        // or everything for one week across sites.
        builder.HasIndex(r => new { r.ProjectId, r.IsoYear, r.IsoWeek });
        builder.HasIndex(r => new { r.IsoYear, r.IsoWeek });
    }
}
