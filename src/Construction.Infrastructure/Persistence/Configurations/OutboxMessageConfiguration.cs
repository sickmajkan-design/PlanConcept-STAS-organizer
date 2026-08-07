using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.LastError)
            .HasMaxLength(2_000);

        // The only query the processor runs: what is due, oldest first.
        // Filtered, so the index holds the backlog rather than the archive —
        // a table that has delivered a million messages and owes nothing has
        // an empty index here.
        builder.HasIndex(m => m.NextAttemptAt)
            .HasFilter("\"SentAt\" IS NULL AND \"AbandonedAt\" IS NULL")
            .HasDatabaseName("ix_outbox_messages_due");

        // Claiming writes a token and then reads the rows back by it.
        builder.HasIndex(m => m.ClaimId)
            .HasFilter("\"ClaimId\" IS NOT NULL")
            .HasDatabaseName("ix_outbox_messages_claim");

        builder.Property(m => m.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // A message cannot be both delivered and given up on. The two are set
        // by different paths, and a row carrying both would leave nobody able
        // to say what happened to it.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_outbox_messages_one_outcome",
            "NOT (\"SentAt\" IS NOT NULL AND \"AbandonedAt\" IS NOT NULL)"));
    }
}
