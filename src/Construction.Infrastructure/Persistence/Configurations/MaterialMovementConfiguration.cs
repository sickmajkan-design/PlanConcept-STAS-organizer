using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class MaterialMovementConfiguration : IEntityTypeConfiguration<MaterialMovement>
{
    public void Configure(EntityTypeBuilder<MaterialMovement> builder)
    {
        builder.ToTable("material_movements");

        builder.HasKey(m => m.Id);

        // Movements of a deleted material, or onto a deleted site, are not
        // chargeable to anything.
        builder.HasQueryFilter(m =>
            !m.Material.IsDeleted && (m.Project == null || !m.Project.IsDeleted));

        builder.Ignore(m => m.SignedQuantity);
        builder.Ignore(m => m.TotalCost);

        // Quantity matches the material's own precision; price is money.
        builder.Property(m => m.Quantity).HasPrecision(18, 3);
        builder.Property(m => m.UnitPrice).HasPrecision(18, 2);

        builder.Property(m => m.Note).HasMaxLength(500);

        builder.HasOne(m => m.Material)
            .WithMany(m => m.Movements)
            .HasForeignKey(m => m.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Project)
            .WithMany(p => p.MaterialMovements)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.RecordedByUser)
            .WithMany()
            .HasForeignKey(m => m.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            // A delivery or an issue carries its direction in Kind, so a
            // negative one would silently move stock the wrong way. A
            // correction is a signed delta by nature and only has to be
            // non-zero. 3 is MaterialMovementKind.Adjustment.
            //
            // A CASE rather than an OR chain: every branch has to yield a
            // real boolean, because a CHECK only rejects on FALSE and lets
            // NULL through.
            t.HasCheckConstraint(
                "ck_material_movements_quantity_signed_by_kind",
                """
                CASE WHEN "Kind" = 3
                     THEN "Quantity" <> 0
                     ELSE "Quantity" > 0
                END
                """);

            t.HasCheckConstraint(
                "ck_material_movements_price_not_negative",
                "\"UnitPrice\" IS NULL OR \"UnitPrice\" >= 0");
        });

        // "What did this material cost, and what went to this site" — the two
        // directions the costing report reads it from.
        builder.HasIndex(m => new { m.MaterialId, m.OccurredOn });
        builder.HasIndex(m => new { m.ProjectId, m.OccurredOn });
    }
}
