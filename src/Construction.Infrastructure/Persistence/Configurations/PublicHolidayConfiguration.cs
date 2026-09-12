using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("public_holidays");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();

        builder.Property(h => h.CountryCode).HasMaxLength(2).IsRequired();

        // Two entries for the same country and date would make "is this day
        // a holiday there" ambiguous for nothing — one row per (country,
        // date) is the whole point. Different countries sharing a date (e.g.
        // 1 January everywhere) are not a conflict.
        builder.HasIndex(h => new { h.CountryCode, h.Date }).IsUnique();
    }
}
