using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LedgerColumnConfiguration : IEntityTypeConfiguration<LedgerColumn>
{
    public void Configure(EntityTypeBuilder<LedgerColumn> builder)
    {
        builder.ToTable("ledger_columns");

        builder.HasKey(c => c.Id);

        // A column of a deleted ledger is not reachable from anywhere.
        builder.HasQueryFilter(c => !c.Ledger.IsDeleted);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(c => c.Ledger)
            .WithMany(l => l.Columns)
            .HasForeignKey(c => c.LedgerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.LedgerId, c.SortOrder });
    }
}
