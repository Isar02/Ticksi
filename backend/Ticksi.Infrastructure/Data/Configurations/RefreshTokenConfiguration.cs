using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshToken.Constraints.TokenHashMaxLength);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        // Revoking only succeeds while the row is still unrevoked, so one token rotates once.
        builder.Property(t => t.RevokedAtUtc)
            .IsConcurrencyToken();

        builder.HasOne(t => t.AppUser)
            .WithMany()
            .HasForeignKey(t => t.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
