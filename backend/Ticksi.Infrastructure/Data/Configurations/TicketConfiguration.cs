using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class TicketConfiguration : BaseEntityConfiguration<Ticket>
{
    public override void Configure(EntityTypeBuilder<Ticket> builder)
    {
        base.Configure(builder);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(Ticket.Constraints.CodeMaxLength);

        builder.HasIndex(t => t.Code)
            .IsUnique();

        builder.HasOne(t => t.OrderItem)
            .WithMany(i => i.Tickets)
            .HasForeignKey(t => t.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
