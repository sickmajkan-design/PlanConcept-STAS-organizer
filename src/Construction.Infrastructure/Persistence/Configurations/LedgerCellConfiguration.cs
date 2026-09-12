using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LedgerCellConfiguration : IEntityTypeConfiguration<LedgerCell>
{
    public void Configure(EntityTypeBuilder<LedgerCell> builder)
    {
        builder.ToTable("ledger_cells");

        builder.HasKey(c => c.Id);

        builder.HasQueryFilter(c => !c.Row.Section.Ledger.IsDeleted);

        builder.Property(c => c.Value)
            .HasMaxLength(2000);

        builder.Property(c => c.ColorTag)
            .HasMaxLength(20);

        builder.HasOne(c => c.Row)
            .WithMany(r => r.Cells)
            .HasForeignKey(c => c.RowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Column)
            .WithMany()
            .HasForeignKey(c => c.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        // One value per (row, column) — the upsert in SetLedgerCellCommand
        // relies on this to decide insert vs. update.
        builder.HasIndex(c => new { c.RowId, c.ColumnId }).IsUnique();
    }
}
