using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
{
    private static readonly DateTime SeededAt = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BookCategory> builder)
    {
        builder.ToTable("book_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Group).HasMaxLength(100);
        builder.Property(c => c.SystemKey).HasMaxLength(50);
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Bucket).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(c => c.SystemKey).IsUnique().HasFilter("\"SystemKey\" IS NOT NULL");
        builder.HasIndex(c => new { c.Kind, c.SortOrder });

        builder.HasData(Seed());
    }

    /// <summary>The categories the settlement and the overview depend on. The ids are fixed so every environment agrees.</summary>
    private static BookCategory[] Seed() =>
    [
        Make("00000000-0000-0000-0b00-000000000001", "Collection", "Naplata klijentu", BookCategoryKind.Income, BookCategoryBucket.Collection, 1),
        Make("00000000-0000-0000-0b00-000000000002", "WorkerPay", "Plata radnika", BookCategoryKind.Cost, BookCategoryBucket.WorkerEarnings, 2),
        Make("00000000-0000-0000-0b00-000000000003", "Subcontractors", "Kooperanti", BookCategoryKind.Cost, BookCategoryBucket.WorkerEarnings, 3),
        Make("00000000-0000-0000-0b00-000000000004", "Office", "Kancelarija", BookCategoryKind.Cost, BookCategoryBucket.WorkerEarnings, 4),
        Make("00000000-0000-0000-0b00-000000000005", "Fuel", "Gorivo", BookCategoryKind.Cost, BookCategoryBucket.SiteCosts, 5),
        Make("00000000-0000-0000-0b00-000000000006", "Housing", "Stanovanje", BookCategoryKind.Cost, BookCategoryBucket.SiteCosts, 6),
        Make("00000000-0000-0000-0b00-000000000007", "CarRental", "Rent a car", BookCategoryKind.Cost, BookCategoryBucket.SiteCosts, 7),
        Make("00000000-0000-0000-0b00-000000000008", "Tools", "Alat", BookCategoryKind.Cost, BookCategoryBucket.SiteCosts, 8),
        Make("00000000-0000-0000-0b00-000000000009", "Materials", "Materijal", BookCategoryKind.Cost, BookCategoryBucket.SiteCosts, 9),
        Make("00000000-0000-0000-0b00-00000000000a", "FixedCosts", "Fiksni troškovi", BookCategoryKind.Cost, BookCategoryBucket.Fixed, 10),
        Make("00000000-0000-0000-0b00-00000000000b", "Insurance", "Osiguranje", BookCategoryKind.Cost, BookCategoryBucket.Fixed, 11),
        Make("00000000-0000-0000-0b00-00000000000c", "Other", "Ostalo", BookCategoryKind.Cost, BookCategoryBucket.Fixed, 99),
    ];

    private static BookCategory Make(
        string id, string key, string name, BookCategoryKind kind, BookCategoryBucket bucket, int order) =>
        new()
        {
            Id = Guid.Parse(id),
            SystemKey = key,
            Name = name,
            Kind = kind,
            Bucket = bucket,
            SortOrder = order,
            IsActive = true,
            CreatedAt = SeededAt,
        };
}
