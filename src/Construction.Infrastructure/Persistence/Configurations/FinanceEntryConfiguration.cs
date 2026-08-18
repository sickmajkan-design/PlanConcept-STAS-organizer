using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class FinanceEntryConfiguration : IEntityTypeConfiguration<FinanceEntry>
{
    public void Configure(EntityTypeBuilder<FinanceEntry> builder)
    {
        builder.ToTable("finance_entries");

        builder.HasKey(e => e.Id);

        // Pay for a deleted employee, or charged to a deleted site, is not
        // chargeable to anything.
        builder.HasQueryFilter(e =>
            !e.Employee.IsDeleted && (e.Project == null || !e.Project.IsDeleted));

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.HoursWorked).HasPrecision(18, 2);

        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.FinanceEntries)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Project)
            .WithMany(p => p.FinanceEntries)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_finance_entries_amount_not_negative", "\"Amount\" >= 0");

            // Hours belong to an hourly payment and nowhere else. Written as a
            // CASE, not an OR chain, so every branch yields a real boolean —
            // an OR chain lets a NULL comparison slip a row past the check.
            // 1 is FinanceEntryKind.WorkerPaymentHourly.
            t.HasCheckConstraint(
                "ck_finance_entries_hours_only_for_hourly",
                """
                CASE WHEN "Kind" = 1
                     THEN "HoursWorked" IS NOT NULL AND "HoursWorked" >= 0
                     ELSE "HoursWorked" IS NULL
                END
                """);
        });

        // "What did we pay this person, and what did this site cost in wages"
        // — the two directions the report reads it from.
        builder.HasIndex(e => new { e.EmployeeId, e.OccurredOn });
        builder.HasIndex(e => new { e.ProjectId, e.OccurredOn });
    }
}
