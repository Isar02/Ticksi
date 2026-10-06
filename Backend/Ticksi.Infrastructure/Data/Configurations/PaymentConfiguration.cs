using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class PaymentConfiguration : BaseEntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.ProviderReference)
            .IsRequired()
            .HasMaxLength(Payment.Constraints.ProviderReferenceMaxLength);

        builder.HasIndex(p => p.ProviderReference)
            .IsUnique();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(Payment.Constraints.CurrencyLength)
            .IsFixedLength();

        // The webhook and the buyer's check can complete the same payment at once; only one may issue the tickets.
        builder.Property(p => p.Status)
            .IsConcurrencyToken();

        builder.HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
