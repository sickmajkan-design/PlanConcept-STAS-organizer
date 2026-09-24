using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.CanViewCustomerTaxDetails)
            .HasDefaultValue(false);

        builder.Property(u => u.FinanceAccess)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(FinanceAccess.None);

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(5);

        builder.HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(u => u.EmployeeId)
            .IsUnique()
            .HasFilter("\"EmployeeId\" IS NOT NULL");

        // Many-to-one, unlike Employee above: a customer is a company, and
        // more than one of their people may reasonably want their own portal
        // login — not the same "at most one account" rule a person gets.
        builder.HasOne(u => u.Customer)
            .WithMany()
            .HasForeignKey(u => u.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_users_reminder_days_positive",
            "\"DocumentExpiryReminderDays\" IS NULL OR \"DocumentExpiryReminderDays\" > 0"));
    }
}
