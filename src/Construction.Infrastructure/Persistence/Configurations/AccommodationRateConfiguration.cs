using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class AccommodationRateConfiguration : IEntityTypeConfiguration<AccommodationRate>
{
    public void Configure(EntityTypeBuilder<AccommodationRate> builder)
    {
        builder.ToTable("accommodation_rates");

        builder.HasKey(r => r.Id);

        // Rates for a deleted accommodation are not chargeable to anything.
        builder.HasQueryFilter(r => !r.Accommodation.IsDeleted);

        builder.Property(r => r.MonthlyAmount).HasPrecision(18, 2);

        builder.Property(r => r.Provider).HasMaxLength(200);

        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Accommodation)
            .WithMany(a => a.Rates)
            .HasForeignKey(r => r.AccommodationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SetByUser)
            .WithMany()
            .HasForeignKey(r => r.SetByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_accommodation_rates_ends_after_start",
                "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            t.HasCheckConstraint(
                "ck_accommodation_rates_positive", "\"MonthlyAmount\" > 0");
        });

        // "What is this apartment costing us on day D" — same shape as the
        // vehicle rental rate's index.
        builder.HasIndex(r => new { r.AccommodationId, r.StartDate });
    }
}
