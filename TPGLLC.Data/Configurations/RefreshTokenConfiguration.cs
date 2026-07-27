using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Data.Configurations;

public sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.JwtId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.DeviceName)
            .HasMaxLength(200);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresUtc)
            .IsRequired();

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.JwtId);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}