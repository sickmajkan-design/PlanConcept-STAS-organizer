using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class ScheduledReportSubscriptionConfiguration
    : IEntityTypeConfiguration<ScheduledReportSubscription>
{
    public void Configure(EntityTypeBuilder<ScheduledReportSubscription> builder)
    {
        builder.ToTable("scheduled_report_subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RecipientEmail).HasMaxLength(320).IsRequired();

        builder.Property(s => s.Language).HasMaxLength(2).IsRequired();

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The only query the sweep runs: what is due, in creation order so a
        // long backlog (should one ever build up) is worked oldest-first.
        builder.HasIndex(s => s.NextRunAtUtc);
    }
}
