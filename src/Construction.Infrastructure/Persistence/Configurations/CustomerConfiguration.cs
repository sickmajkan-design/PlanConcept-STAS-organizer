using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.ContactPerson)
            .HasMaxLength(256);

        builder.Property(c => c.Phone)
            .HasMaxLength(64);

        builder.Property(c => c.Email)
            .HasMaxLength(256);

        builder.Property(c => c.Note)
            .HasMaxLength(2000);

        builder.Property(c => c.TaxId)
            .HasMaxLength(64);

        builder.Property(c => c.RegistrationNumber)
            .HasMaxLength(64);

        builder.Property(c => c.VatNumber)
            .HasMaxLength(64);

        builder.HasIndex(c => c.Name);
    }
}
