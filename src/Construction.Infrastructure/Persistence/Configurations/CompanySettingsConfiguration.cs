using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("company_settings");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.TaxId)
            .HasMaxLength(64);

        builder.Property(c => c.RegistrationNumber)
            .HasMaxLength(64);

        builder.Property(c => c.VatNumber)
            .HasMaxLength(64);

        builder.Property(c => c.Phone)
            .HasMaxLength(32);

        builder.Property(c => c.Email)
            .HasMaxLength(256);

        builder.Property(c => c.LogoStorageKey)
            .HasMaxLength(256);

        builder.Property(c => c.LogoContentType)
            .HasMaxLength(128);
    }
}
