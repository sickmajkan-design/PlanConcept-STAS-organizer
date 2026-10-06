using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class FuelTransactionConfiguration : IEntityTypeConfiguration<FuelTransaction>
{
    public void Configure(EntityTypeBuilder<FuelTransaction> builder)
    {
        builder.ToTable("fuel_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CardNumber).HasMaxLength(64).IsRequired();
        builder.Property(t => t.StatementVehicleLabel).HasMaxLength(64);
        builder.Property(t => t.ProductGroup).HasMaxLength(100);
        builder.Property(t => t.ProductType).HasMaxLength(100);
        builder.Property(t => t.ProductCode).HasMaxLength(32).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Country).HasMaxLength(8);
        builder.Property(t => t.Amount).HasPrecision(12, 2);
        builder.Property(t => t.IssueDetail).HasMaxLength(500);
        builder.Property(t => t.ResolutionNote).HasMaxLength(1000);

        // The identity of a statement line; see the entity's remarks.
        builder.HasIndex(t => new { t.CardNumber, t.OccurredOn, t.OccurredAtTime, t.ProductCode, t.Amount })
            .IsUnique();

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.VehicleId);
        builder.HasIndex(t => t.VehicleExpenseId);

        builder.HasOne(t => t.ImportBatch)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Vehicle)
            .WithMany()
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.VehicleExpense)
            .WithMany()
            .HasForeignKey(t => t.VehicleExpenseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class FuelImportBatchConfiguration : IEntityTypeConfiguration<FuelImportBatch>
{
    public void Configure(EntityTypeBuilder<FuelImportBatch> builder)
    {
        builder.ToTable("fuel_import_batches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName).HasMaxLength(260).IsRequired();
    }
}
