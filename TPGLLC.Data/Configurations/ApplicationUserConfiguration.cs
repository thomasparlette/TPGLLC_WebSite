using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Data.Configurations;

public sealed class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.DisplayName)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedUtc)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(x => x.Email);

        builder.HasIndex(x => x.NormalizedEmail);

        builder.HasIndex(x => x.NormalizedUserName);
    }
}