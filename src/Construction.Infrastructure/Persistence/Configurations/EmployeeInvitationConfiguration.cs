using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class EmployeeInvitationConfiguration : IEntityTypeConfiguration<EmployeeInvitation>
{
    public void Configure(EntityTypeBuilder<EmployeeInvitation> builder)
    {
        builder.ToTable("employee_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        // Looked up by hash on every accept and preview.
        builder.HasIndex(i => i.TokenHash).IsUnique();

        // An invitation is meaningless without its employee, so it goes when
        // the employee row does.
        builder.HasOne(i => i.Employee)
            .WithMany()
            .HasForeignKey(i => i.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.EmployeeId);
    }
}
