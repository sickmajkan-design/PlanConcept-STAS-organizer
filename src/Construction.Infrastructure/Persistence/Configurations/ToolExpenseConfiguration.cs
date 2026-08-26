using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ToolExpenseConfiguration : IEntityTypeConfiguration<ToolExpense>
{
    public void Configure(EntityTypeBuilder<ToolExpense> builder)
    {
        builder.ToTable("tool_expenses");

        builder.HasKey(e => e.Id);

        builder.HasQueryFilter(e => !e.Tool.IsDeleted);

        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.Property(e => e.Supplier).HasMaxLength(200);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasOne(e => e.Tool)
            .WithMany(t => t.Expenses)
            .HasForeignKey(e => e.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_tool_expenses_amount_not_negative", "\"Amount\" >= 0"));

        // "What has this tool cost, over this period" — the only way the
        // report reads it.
        builder.HasIndex(e => new { e.ToolId, e.OccurredOn });
        builder.HasIndex(e => e.Kind);
    }
}
