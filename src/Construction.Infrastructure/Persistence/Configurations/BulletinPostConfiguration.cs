using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class BulletinPostConfiguration : IEntityTypeConfiguration<BulletinPost>
{
    public void Configure(EntityTypeBuilder<BulletinPost> builder)
    {
        builder.ToTable("bulletin_posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Body).HasMaxLength(4000).IsRequired();

        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Newest first is the only order the board is ever read in.
        builder.HasIndex(p => p.CreatedAt).IsDescending();
    }
}
