using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class NotificationGroupMemberConfiguration : IEntityTypeConfiguration<NotificationGroupMember>
{
    public void Configure(EntityTypeBuilder<NotificationGroupMember> builder)
    {
        builder.ToTable("notification_group_members");

        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.NotificationGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.NotificationGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Employee)
            .WithMany()
            .HasForeignKey(m => m.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.NotificationGroupId, m.EmployeeId }).IsUnique();
    }
}
