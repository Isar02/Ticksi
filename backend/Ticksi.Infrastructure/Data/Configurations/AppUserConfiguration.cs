using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class AppUserConfiguration : BaseEntityConfiguration<AppUser>
{
    public override void Configure(EntityTypeBuilder<AppUser> builder)
    {
        base.Configure(builder);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(AppUser.Constraints.FirstNameMaxLength);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(AppUser.Constraints.LastNameMaxLength);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(AppUser.Constraints.EmailMaxLength);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(AppUser.Constraints.PasswordHashMaxLength);

        builder.Property(u => u.Phone)
            .IsRequired()
            .HasMaxLength(AppUser.Constraints.PhoneMaxLength);

        builder.HasOne(u => u.Role)
            .WithMany(r => r.AppUsers)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
