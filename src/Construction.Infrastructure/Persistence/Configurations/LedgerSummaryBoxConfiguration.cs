using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LedgerSummaryBoxConfiguration : IEntityTypeConfiguration<LedgerSummaryBox>
{
    public void Configure(EntityTypeBuilder<LedgerSummaryBox> builder)
    {
        builder.ToTable("ledger_summary_boxes");

        builder.HasKey(b => b.Id);

        builder.HasQueryFilter(b => !b.Ledger.IsDeleted);

        builder.Property(b => b.Label)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.ManualValue)
            .HasPrecision(18, 2);

        builder.Property(b => b.Color)
            .HasMaxLength(20);

        builder.HasOne(b => b.Ledger)
            .WithMany(l => l.SummaryBoxes)
            .HasForeignKey(b => b.LedgerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting the referenced column just turns this box manual again —
        // its own figures should not vanish because a column was renamed away.
        builder.HasOne(b => b.SourceColumn)
            .WithMany()
            .HasForeignKey(b => b.SourceColumnId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(b => new { b.LedgerId, b.SortOrder });
    }
}
