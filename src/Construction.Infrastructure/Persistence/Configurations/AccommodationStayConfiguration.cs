using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class AccommodationStayConfiguration : IEntityTypeConfiguration<AccommodationStay>
{
    public void Configure(EntityTypeBuilder<AccommodationStay> builder)
    {
        builder.ToTable("accommodation_stays");

        builder.HasKey(s => s.Id);

        // A stay in a deleted accommodation, or by a deleted employee, is not occupancy.
        builder.HasQueryFilter(s => !s.Accommodation.IsDeleted && !s.Employee.IsDeleted);

        builder.Property(s => s.Note).HasMaxLength(500);

        builder.HasOne(s => s.Accommodation)
            .WithMany(a => a.Stays)
            .HasForeignKey(s => s.AccommodationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_accommodation_stays_ends_after_start",
            "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));

        // "Who lives here" and "where does this person live", both by date.
        builder.HasIndex(s => new { s.AccommodationId, s.StartDate });
        builder.HasIndex(s => new { s.EmployeeId, s.StartDate });
    }
}
