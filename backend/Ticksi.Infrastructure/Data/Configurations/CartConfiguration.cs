using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class CartConfiguration : BaseEntityConfiguration<Cart>
{
    public override void Configure(EntityTypeBuilder<Cart> builder)
    {
        base.Configure(builder);

        builder.HasOne(c => c.AppUser)
            .WithOne()
            .HasForeignKey<Cart>(c => c.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
