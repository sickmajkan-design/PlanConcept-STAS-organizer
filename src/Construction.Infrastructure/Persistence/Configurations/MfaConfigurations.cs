using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class MfaRecoveryCodeConfiguration : IEntityTypeConfiguration<MfaRecoveryCode>
{
    public void Configure(EntityTypeBuilder<MfaRecoveryCode> builder)
    {
        builder.ToTable("mfa_recovery_codes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CodeHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(c => c.CodeHash)
            .IsUnique();

        builder.HasOne(c => c.User)
            .WithMany(u => u.MfaRecoveryCodes)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
    }
}

public class MfaChallengeConfiguration : IEntityTypeConfiguration<MfaChallenge>
{
    public void Configure(EntityTypeBuilder<MfaChallenge> builder)
    {
        builder.ToTable("mfa_challenges");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(c => c.TokenHash)
            .IsUnique();

        builder.HasOne(c => c.User)
            .WithMany(u => u.MfaChallenges)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.ExpiresAt);
    }
}
