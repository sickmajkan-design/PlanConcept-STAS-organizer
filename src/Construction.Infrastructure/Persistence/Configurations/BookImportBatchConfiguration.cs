using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class BookImportBatchConfiguration : IEntityTypeConfiguration<BookImportBatch>
{
    public void Configure(EntityTypeBuilder<BookImportBatch> builder)
    {
        builder.ToTable("book_import_batches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName).HasMaxLength(256).IsRequired();
        builder.Property(b => b.Status).HasMaxLength(20).IsRequired();

        builder.HasOne(b => b.RecordedByUser)
            .WithMany()
            .HasForeignKey(b => b.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(b => new { b.TargetYear, b.TargetMonth });
    }
}
