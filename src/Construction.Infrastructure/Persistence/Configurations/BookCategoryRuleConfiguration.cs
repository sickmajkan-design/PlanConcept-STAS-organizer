using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class BookCategoryRuleConfiguration : IEntityTypeConfiguration<BookCategoryRule>
{
    public void Configure(EntityTypeBuilder<BookCategoryRule> builder)
    {
        builder.ToTable("book_category_rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Text).HasMaxLength(100).IsRequired();

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.Priority);
    }
}
