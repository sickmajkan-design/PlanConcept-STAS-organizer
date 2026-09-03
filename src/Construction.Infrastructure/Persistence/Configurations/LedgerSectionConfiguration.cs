using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LedgerSectionConfiguration : IEntityTypeConfiguration<LedgerSection>
{
    public void Configure(EntityTypeBuilder<LedgerSection> builder)
    {
        builder.ToTable("ledger_sections");

        builder.HasKey(s => s.Id);

        builder.HasQueryFilter(s => !s.Ledger.IsDeleted);

        builder.Property(s => s.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasOne(s => s.Ledger)
            .WithMany(l => l.Sections)
            .HasForeignKey(s => s.LedgerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting the referenced project must not take this section's
        // history down with it — it just stops pointing at anything real.
        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.LedgerId, s.SortOrder });
    }
}
