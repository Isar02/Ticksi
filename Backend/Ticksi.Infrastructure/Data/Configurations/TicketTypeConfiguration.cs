using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_TicketTypes_QuantityReserved",
            "[QuantityReserved] >= 0 AND [QuantityReserved] <= [Quantity]"));

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(TicketType.Constraints.NameMaxLength);

        builder.Property(t => t.Price)
            .HasPrecision(18, 2);

        // Two buyers taking the last tickets at once: the second save fails instead of overselling.
        builder.Property(t => t.QuantityReserved)
            .IsConcurrencyToken();

        builder.HasIndex(t => new { t.EventId, t.Name })
            .IsUnique();

        builder.HasOne(t => t.Event)
            .WithMany(e => e.TicketTypes)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
