using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Data.Configurations;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("CustomerProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Phone)
            .HasMaxLength(30);

        builder.Property(x => x.Company)
            .HasMaxLength(120);

        builder.Property(x => x.Address1)
            .HasMaxLength(150);

        builder.Property(x => x.Address2)
            .HasMaxLength(150);

        builder.Property(x => x.City)
            .HasMaxLength(80);

        builder.Property(x => x.State)
            .HasMaxLength(40);

        builder.Property(x => x.ZipCode)
            .HasMaxLength(20);

        builder.Property(x => x.Country)
            .HasMaxLength(80);

        builder.Property(x => x.PreferredContactMethod)
            .HasMaxLength(30);

        builder.Property(x => x.ReceiveEmail)
            .HasDefaultValue(true);

        builder.Property(x => x.ReceiveSms)
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.ApplicationUserId)
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}