using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class GeneralExpenseConfiguration : IEntityTypeConfiguration<GeneralExpense>
{
    public void Configure(EntityTypeBuilder<GeneralExpense> builder)
    {
        builder.ToTable("general_expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.Property(e => e.Supplier).HasMaxLength(200);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_general_expenses_amount_not_negative", "\"Amount\" >= 0"));

        builder.HasIndex(e => new { e.Category, e.OccurredOn });
        builder.HasIndex(e => e.ProjectId);
        builder.HasIndex(e => e.EmployeeId);
    }
}
