using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.HasOne(c => c.AppUser)
            .WithOne()
            .HasForeignKey<Cart>(c => c.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
