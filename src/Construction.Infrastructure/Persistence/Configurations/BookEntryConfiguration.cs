using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class BookEntryConfiguration : IEntityTypeConfiguration<BookEntry>
{
    public void Configure(EntityTypeBuilder<BookEntry> builder)
    {
        builder.ToTable("book_entries");

        builder.HasKey(e => e.Id);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Counterparty).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint("ck_book_entries_amount_positive", "\"Amount\" > 0"));

        builder.HasIndex(e => e.OccurredOn);
        builder.HasIndex(e => new { e.CategoryId, e.OccurredOn });
        builder.HasIndex(e => new { e.ProjectId, e.OccurredOn });

        // A client that retries the same entry (a phone that was offline) must not create it twice.
        builder.HasIndex(e => e.ClientRequestId).IsUnique().HasFilter("\"ClientRequestId\" IS NOT NULL");

        builder.HasOne(e => e.RecurringExpense)
            .WithMany()
            .HasForeignKey(e => e.RecurringExpenseId)
            .OnDelete(DeleteBehavior.SetNull);

        // A template makes one entry per period, whichever instance of the API runs the job and however many
        // times. A deleted entry keeps its place, so a cost someone removed on purpose does not come back.
        builder.HasIndex(e => new { e.RecurringExpenseId, e.PeriodYear, e.PeriodMonth })
            .IsUnique()
            .HasFilter("\"RecurringExpenseId\" IS NOT NULL");

        // A template or import row produces at most one entry.
        builder.HasIndex(e => new { e.Source, e.SourceId }).IsUnique().HasFilter("\"SourceId\" IS NOT NULL");
    }
}
