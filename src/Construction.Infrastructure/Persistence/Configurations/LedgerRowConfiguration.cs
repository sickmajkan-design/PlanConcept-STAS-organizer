using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LedgerRowConfiguration : IEntityTypeConfiguration<LedgerRow>
{
    public void Configure(EntityTypeBuilder<LedgerRow> builder)
    {
        builder.ToTable("ledger_rows");

        builder.HasKey(r => r.Id);

        builder.HasQueryFilter(r => !r.Section.Ledger.IsDeleted);

        builder.Property(r => r.Label)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasOne(r => r.Section)
            .WithMany(s => s.Rows)
            .HasForeignKey(r => r.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.SectionId, r.SortOrder });
    }
}
