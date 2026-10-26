using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ToolKitItemConfiguration : IEntityTypeConfiguration<ToolKitItem>
{
    public void Configure(EntityTypeBuilder<ToolKitItem> builder)
    {
        builder.ToTable("tool_kit_items");

        builder.HasKey(i => i.Id);

        builder.HasQueryFilter(i => !i.Tool.IsDeleted);

        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);

        builder.HasOne(i => i.Tool)
            .WithMany(t => t.KitItems)
            .HasForeignKey(i => i.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tool_kit_items_quantity_positive", "\"Quantity\" >= 1");
            t.HasCheckConstraint("ck_tool_kit_items_price_not_negative", "\"UnitPrice\" >= 0");
        });

        builder.HasIndex(i => new { i.ToolId, i.SortOrder });
    }
}

public class ToolKitEventConfiguration : IEntityTypeConfiguration<ToolKitEvent>
{
    public void Configure(EntityTypeBuilder<ToolKitEvent> builder)
    {
        builder.ToTable("tool_kit_events");

        builder.HasKey(e => e.Id);

        builder.HasQueryFilter(e => !e.Tool.IsDeleted);

        builder.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.HasOne(e => e.Tool)
            .WithMany(t => t.KitEvents)
            .HasForeignKey(e => e.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.ToolId, e.CreatedAt });
        // What is still owed, per worker: the open charges and refunds.
        builder.HasIndex(e => new { e.EmployeeId, e.SettledAt });
    }
}
