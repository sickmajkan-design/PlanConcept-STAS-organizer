using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ToolRentalRateConfiguration : IEntityTypeConfiguration<ToolRentalRate>
{
    public void Configure(EntityTypeBuilder<ToolRentalRate> builder)
    {
        builder.ToTable("tool_rental_rates");

        builder.HasKey(r => r.Id);

        // Rates for a deleted tool are not chargeable to anything.
        builder.HasQueryFilter(r => !r.Tool.IsDeleted);

        builder.Property(r => r.MonthlyAmount).HasPrecision(18, 2);

        builder.Property(r => r.Provider).HasMaxLength(200);

        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Tool)
            .WithMany(t => t.RentalRates)
            .HasForeignKey(r => r.ToolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SetByUser)
            .WithMany()
            .HasForeignKey(r => r.SetByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_tool_rental_rates_ends_after_start",
                "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            t.HasCheckConstraint(
                "ck_tool_rental_rates_positive", "\"MonthlyAmount\" > 0");
        });

        // "What is this tool costing us on day D" — the same join
        // VehicleRentalRate's equivalent index describes, for tools instead
        // of the fleet.
        builder.HasIndex(r => new { r.ToolId, r.StartDate });
    }
}
