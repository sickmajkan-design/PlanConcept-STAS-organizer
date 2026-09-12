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

        builder.Property(r => r.ColorTag)
            .HasMaxLength(20);

        builder.HasOne(r => r.Section)
            .WithMany(s => s.Rows)
            .HasForeignKey(r => r.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Tool)
            .WithMany()
            .HasForeignKey(r => r.ToolId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Material)
            .WithMany()
            .HasForeignKey(r => r.MaterialId)
            .OnDelete(DeleteBehavior.SetNull);

        // Set once a row is pushed through the real form — never cascades
        // (deleting the created expense/rate should not silently delete the
        // ledger row that pointed at it).
        builder.HasOne(r => r.PromotedGeneralExpense)
            .WithMany()
            .HasForeignKey(r => r.PromotedGeneralExpenseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.PromotedAccommodationRate)
            .WithMany()
            .HasForeignKey(r => r.PromotedAccommodationRateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.SectionId, r.SortOrder });
    }
}
