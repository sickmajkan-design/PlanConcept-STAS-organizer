using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.EmployeeNumber)
            .HasMaxLength(32)
            .IsRequired();

        // Unique among non-deleted rows so a number can be reused after
        // the previous holder is soft-deleted.
        builder.HasIndex(e => e.EmployeeNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Phone)
            .HasMaxLength(32);

        builder.Property(e => e.Email)
            .HasMaxLength(256);

        builder.Property(e => e.Address)
            .HasMaxLength(512);

        builder.Property(e => e.Position)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.AnnualLeaveDaysAllowance)
            .HasDefaultValue(20);

        builder.Ignore(e => e.FullName);

        builder.HasIndex(e => e.LastName);
        builder.HasIndex(e => e.Status);
    }
}
