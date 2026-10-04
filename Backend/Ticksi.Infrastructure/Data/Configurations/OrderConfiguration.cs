using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasOne(o => o.AppUser)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
