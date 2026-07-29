using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LocationRecordConfiguration : IEntityTypeConfiguration<LocationRecord>
{
    public void Configure(EntityTypeBuilder<LocationRecord> builder)
    {
        builder.ToTable("location_records");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .UseIdentityAlwaysColumn();

        builder.HasOne(l => l.Employee)
            .WithMany(e => e.LocationRecords)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Location history is kept even for soft-deleted employees, but the
        // employee navigation carries a query filter, so mirror it here to
        // keep EF's filter expectations consistent.
        builder.HasQueryFilter(l => !l.Employee.IsDeleted);

        // Covers "history for employee X in time range" and
        // "last known location" (ORDER BY Timestamp DESC LIMIT 1).
        builder.HasIndex(l => new { l.EmployeeId, l.Timestamp })
            .IsDescending(false, true);

        builder.HasIndex(l => l.Timestamp);
    }
}
