using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class EventConfiguration : BaseEntityConfiguration<Event>
{
    public override void Configure(EntityTypeBuilder<Event> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(Event.Constraints.NameMaxLength);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(Event.Constraints.DescriptionMaxLength);

        builder.Property(e => e.Contact)
            .IsRequired()
            .HasMaxLength(Event.Constraints.ContactMaxLength);

        // Two poster changes at once: the second save fails, so each replaced file is removed only once.
        builder.Property(e => e.PosterUrl)
            .HasMaxLength(Event.Constraints.PosterUrlMaxLength)
            .IsConcurrencyToken();

        builder.HasIndex(e => e.Date);

        builder.HasOne(e => e.AppUser)
            .WithMany()
            .HasForeignKey(e => e.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OrganizerCompany)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.OrganizerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventType)
            .WithMany(t => t.Events)
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventCategory)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.EventCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany(l => l.Events)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
