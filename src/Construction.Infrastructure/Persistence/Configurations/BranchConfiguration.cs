using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();

        builder.Property(b => b.Color).HasMaxLength(7).IsRequired();

        builder.Property(b => b.Kind)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(Construction.Domain.Enums.BranchKind.LegalEntity);

        builder.Property(b => b.LegalName).HasMaxLength(300);
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.City).HasMaxLength(120);
        builder.Property(b => b.PostalCode).HasMaxLength(20);
        builder.Property(b => b.CountryCode).HasMaxLength(2);
        builder.Property(b => b.TaxId).HasMaxLength(50);
        builder.Property(b => b.RegistrationNumber).HasMaxLength(50);
        builder.Property(b => b.VatNumber).HasMaxLength(50);
        builder.Property(b => b.OwnerName).HasMaxLength(200);
        builder.Property(b => b.ContactPerson).HasMaxLength(200);
        builder.Property(b => b.Phone).HasMaxLength(50);
        builder.Property(b => b.Email).HasMaxLength(200);
        builder.Property(b => b.Note).HasMaxLength(2000);

        builder.HasIndex(b => b.Name).IsUnique();
    }
}
