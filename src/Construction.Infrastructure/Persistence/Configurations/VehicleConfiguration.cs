using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.Property(v => v.Brand)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Model)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.RegistrationNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(v => v.RegistrationNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(v => v.Vin)
            .HasMaxLength(32);

        builder.HasIndex(v => v.Vin)
            .IsUnique()
            .HasFilter("\"Vin\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.Property(v => v.QrCode)
            .HasMaxLength(256);

        builder.HasIndex(v => v.QrCode)
            .IsUnique()
            .HasFilter("\"QrCode\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasOne(v => v.AssignedEmployee)
            .WithMany(e => e.AssignedVehicles)
            .HasForeignKey(v => v.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(v => v.AssignedProject)
            .WithMany(p => p.AssignedVehicles)
            .HasForeignKey(v => v.AssignedProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.AssignedEmployeeId);
        builder.HasIndex(v => v.AssignedProjectId);
        builder.HasIndex(v => v.Status);
    }
}
