using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class LocationRecordConfiguration : IEntityTypeConfiguration<LocationRecord>
{
    public void Configure(EntityTypeBuilder<LocationRecord> builder)
    {
        builder.ToTable("location_records");

        // (Id, Timestamp), not Id.
        //
        // The table is partitioned by month on Timestamp, and PostgreSQL
        // requires the partition key to be part of every unique constraint —
        // it cannot enforce uniqueness across partitions it would have to scan
        // all of. Id alone is still unique in practice because it comes from
        // one identity sequence shared by every partition; the composite key
        // is what makes that enforceable.
        //
        // Nothing loads a ping by its id — they are written once and read by
        // employee and time range — so this costs no call site.
        builder.HasKey(l => new { l.Id, l.Timestamp });

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
