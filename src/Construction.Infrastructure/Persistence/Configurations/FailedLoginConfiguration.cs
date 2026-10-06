using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class FailedLoginConfiguration : IEntityTypeConfiguration<FailedLogin>
{
    public void Configure(EntityTypeBuilder<FailedLogin> builder)
    {
        builder.ToTable("failed_logins");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Email).HasMaxLength(256).IsRequired();
        builder.Property(f => f.Reason).HasMaxLength(32).IsRequired();
        builder.Property(f => f.IpAddress).HasMaxLength(64);
        builder.Property(f => f.UserAgent).HasMaxLength(200);

        // No foreign key: an attempt against an account that is later deleted
        // (or never existed) must still be on record.
        builder.HasIndex(f => f.OccurredAt);
    }
}
