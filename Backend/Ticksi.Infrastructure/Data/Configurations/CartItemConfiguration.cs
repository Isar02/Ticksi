using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class CartItemConfiguration : BaseEntityConfiguration<CartItem>
{
    public override void Configure(EntityTypeBuilder<CartItem> builder)
    {
        base.Configure(builder);

        builder.HasIndex(i => new { i.CartId, i.TicketTypeId })
            .IsUnique();

        builder.HasOne(i => i.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.TicketType)
            .WithMany()
            .HasForeignKey(i => i.TicketTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
