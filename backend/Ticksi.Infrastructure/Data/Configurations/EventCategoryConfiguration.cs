using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class EventCategoryConfiguration : BaseEntityConfiguration<EventCategory>
{
    public override void Configure(EntityTypeBuilder<EventCategory> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(EventCategory.Constraints.NameMaxLength);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(EventCategory.Constraints.DescriptionMaxLength);

        builder.Property(c => c.PosterUrl)
            .IsRequired()
            .HasMaxLength(EventCategory.Constraints.PosterUrlMaxLength);
    }
}
