using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LedgerConfiguration : IEntityTypeConfiguration<Ledger>
{
    public void Configure(EntityTypeBuilder<Ledger> builder)
    {
        builder.ToTable("ledgers");

        builder.HasKey(l => l.Id);

        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.Property(l => l.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.Note)
            .HasMaxLength(2000);

        builder.HasOne(l => l.CreatedByUser)
            .WithMany()
            .HasForeignKey(l => l.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.Year, l.Month });
    }
}
