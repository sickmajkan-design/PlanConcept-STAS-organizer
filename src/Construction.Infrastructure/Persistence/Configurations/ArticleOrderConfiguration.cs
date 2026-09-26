using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ArticleOrderConfiguration : IEntityTypeConfiguration<ArticleOrder>
{
    public void Configure(EntityTypeBuilder<ArticleOrder> builder)
    {
        builder.ToTable("article_orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Note).HasMaxLength(1000);
        builder.Property(o => o.ReviewNote).HasMaxLength(1000);

        builder.HasOne(o => o.RequestedByUser)
            .WithMany()
            .HasForeignKey(o => o.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Employee)
            .WithMany()
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Project)
            .WithMany()
            .HasForeignKey(o => o.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.HandledByUser)
            .WithMany()
            .HasForeignKey(o => o.HandledByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.ArticleOrder)
            .HasForeignKey(i => i.ArticleOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => new { o.RequestedByUserId, o.CreatedAt });

        // Two people moving the same request on at once: the second is told, not overwritten.
        builder.Property<uint>("Version").IsRowVersion();
    }
}

public class ArticleOrderItemConfiguration : IEntityTypeConfiguration<ArticleOrderItem>
{
    public void Configure(EntityTypeBuilder<ArticleOrderItem> builder)
    {
        builder.ToTable("article_order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Unit).HasMaxLength(30);
        builder.Property(i => i.Note).HasMaxLength(300);
        builder.Property(i => i.Quantity).HasPrecision(12, 2);

        builder.ToTable(t => t.HasCheckConstraint("ck_article_order_items_quantity", "\"Quantity\" > 0"));
    }
}
