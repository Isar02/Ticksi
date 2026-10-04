using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
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
