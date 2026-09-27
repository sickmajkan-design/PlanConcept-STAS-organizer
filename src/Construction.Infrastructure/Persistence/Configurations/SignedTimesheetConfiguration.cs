using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class SignedTimesheetConfiguration : IEntityTypeConfiguration<SignedTimesheet>
{
    public void Configure(EntityTypeBuilder<SignedTimesheet> builder)
    {
        builder.ToTable("signed_timesheets");

        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_signed_timesheets_iso_week", "\"IsoWeek\" BETWEEN 1 AND 53"));

        builder.HasIndex(s => new { s.ProjectId, s.Year, s.IsoWeek }).IsUnique();

        builder.Property<uint>("Version").IsRowVersion();
    }
}
