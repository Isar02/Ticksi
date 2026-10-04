using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class NotificationConfiguration : BaseEntityConfiguration<Notification>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(Notification.Constraints.TitleMaxLength);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(Notification.Constraints.MessageMaxLength);

        builder.HasOne(n => n.AppUser)
            .WithMany()
            .HasForeignKey(n => n.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
