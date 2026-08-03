using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class AbsenceConfiguration : IEntityTypeConfiguration<Absence>
{
    public void Configure(EntityTypeBuilder<Absence> builder)
    {
        builder.ToTable("absences");

        builder.HasKey(a => a.Id);

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Ignore(a => a.DayCount);

        builder.Property(a => a.Reason).HasMaxLength(1000);
        builder.Property(a => a.ReviewNote).HasMaxLength(1000);

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Absences)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.RequestedByUser)
            .WithMany()
            .HasForeignKey(a => a.RequestedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.ReviewedByUser)
            .WithMany()
            .HasForeignKey(a => a.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_absences_ends_after_start", "\"EndDate\" >= \"StartDate\""));

        // "Is this person away on day D", the question the schedule asks for
        // every employee in the week.
        builder.HasIndex(a => new { a.EmployeeId, a.StartDate });
        builder.HasIndex(a => a.Status);
    }
}
