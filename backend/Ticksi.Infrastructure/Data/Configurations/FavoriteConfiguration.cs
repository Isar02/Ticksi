using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class FavoriteConfiguration : BaseEntityConfiguration<Favorite>
{
    public override void Configure(EntityTypeBuilder<Favorite> builder)
    {
        base.Configure(builder);

        builder.HasIndex(f => new { f.AppUserId, f.EventId })
            .IsUnique();

        builder.HasOne(f => f.AppUser)
            .WithMany()
            .HasForeignKey(f => f.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Event)
            .WithMany()
            .HasForeignKey(f => f.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
