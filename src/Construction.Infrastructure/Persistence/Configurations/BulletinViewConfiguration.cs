using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Construction.Infrastructure.Persistence.Configurations;

public class BulletinViewConfiguration : IEntityTypeConfiguration<BulletinView>
{
    public void Configure(EntityTypeBuilder<BulletinView> builder)
    {
        builder.ToTable("bulletin_views");

        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.BulletinPost)
            .WithMany(p => p.Views)
            .HasForeignKey(v => v.BulletinPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One first-look per user per post; a second view changes nothing.
        builder.HasIndex(v => new { v.BulletinPostId, v.UserId }).IsUnique();
    }
}
