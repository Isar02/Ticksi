using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class EventTypeConfiguration : BaseEntityConfiguration<EventType>
{
    public override void Configure(EntityTypeBuilder<EventType> builder)
    {
        base.Configure(builder);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(EventType.Constraints.NameMaxLength);

        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(EventType.Constraints.DescriptionMaxLength);
    }
}
