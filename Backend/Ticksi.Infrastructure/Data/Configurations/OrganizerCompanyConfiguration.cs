using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class OrganizerCompanyConfiguration : BaseEntityConfiguration<OrganizerCompany>
{
    public override void Configure(EntityTypeBuilder<OrganizerCompany> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(OrganizerCompany.Constraints.NameMaxLength);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(OrganizerCompany.Constraints.EmailMaxLength);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(OrganizerCompany.Constraints.PhoneMaxLength);

        builder.Property(c => c.Address)
            .IsRequired()
            .HasMaxLength(OrganizerCompany.Constraints.AddressMaxLength);

        builder.Property(c => c.TaxId)
            .IsRequired()
            .HasMaxLength(OrganizerCompany.Constraints.TaxIdMaxLength);
    }
}
