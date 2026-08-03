using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class CustomerVehicleConfiguration
    : IEntityTypeConfiguration<CustomerVehicle>
{
    public void Configure(EntityTypeBuilder<CustomerVehicle> builder)
    {
        builder.HasKey(x => x.Id);

      
        builder.Property(x => x.Make)
            .HasMaxLength(120);

        builder.Property(x => x.Model)
            .HasMaxLength(120);

        builder.Property(x => x.Vin)
            .HasMaxLength(17);

        builder.Property(x => x.Nickname)
            .HasMaxLength(100);

        builder.Property(x => x.LicensePlate)
            .HasMaxLength(25);

        builder.Property(x => x.PhotoPath)
            .HasMaxLength(400);

        builder.HasIndex(x => x.CustomerId);

        builder.HasIndex(x => x.Vin);

        builder.HasIndex(x => new
        {
            x.CustomerId,
            x.IsPrimary
        });

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}