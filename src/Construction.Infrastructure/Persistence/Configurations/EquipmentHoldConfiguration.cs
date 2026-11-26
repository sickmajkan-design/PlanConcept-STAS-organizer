using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EquipmentHoldConfiguration : IEntityTypeConfiguration<EquipmentHold>
{
    public void Configure(EntityTypeBuilder<EquipmentHold> builder)
    {
        builder.ToTable("equipment_holds");
        builder.HasKey(h => h.Id);

        builder.HasIndex(h => new { h.Kind, h.ItemId, h.From });
        // Everybody's items on one day: the planning screen asks by period, not by item.
        builder.HasIndex(h => new { h.From, h.To });
    }
}

public class PlannedEquipmentChangeConfiguration : IEntityTypeConfiguration<PlannedEquipmentChange>
{
    public void Configure(EntityTypeBuilder<PlannedEquipmentChange> builder)
    {
        builder.ToTable("planned_equipment_changes");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.EffectiveFrom);
        builder.HasIndex(c => new { c.Kind, c.ItemId });
    }
}
