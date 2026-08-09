using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.Endpoint)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(r => r.RequestHash)
            .HasMaxLength(64)
            .IsRequired();

        // The response body is arbitrary JSON of arbitrary length; no limit.
        builder.Property(r => r.ResponseBody)
            .HasColumnType("text");

        // This index is the mechanism, not an optimisation. Two concurrent
        // retries both look, both find nothing, and both try to insert — and
        // the database refuses the second. Without it the whole scheme is a
        // check-then-act race, which is precisely the situation a retry
        // creates.
        //
        // Scoped by user as well as key: keys are chosen by clients, two
        // clients can pick the same one, and a replay across accounts would
        // hand somebody else's response to whoever guessed it.
        builder.HasIndex(r => new { r.UserId, r.Key })
            .IsUnique();

        // For the retention sweep.
        builder.HasIndex(r => r.CreatedAt);
    }
}
