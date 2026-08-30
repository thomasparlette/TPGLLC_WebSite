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

        builder.Property(x => x.Submodel)
            .HasMaxLength(120);

        builder.Property(x => x.BodyStyle)
            .HasMaxLength(80);

        builder.Property(x => x.EngineFuel)
            .HasMaxLength(160);

        builder.Property(x => x.Transmission)
            .HasMaxLength(120);

        builder.Property(x => x.DriveType)
            .HasMaxLength(60);

        builder.Property(x => x.Brake)
            .HasMaxLength(80);

        builder.Property(x => x.Gvw)
            .HasMaxLength(40);

        builder.Property(x => x.Vin)
            .HasMaxLength(17);

        builder.Property(x => x.Nickname)
            .HasMaxLength(100);

        builder.Property(x => x.LicensePlate)
            .HasMaxLength(25);

        builder.Property(x => x.StateProvince)
            .HasMaxLength(50);

        builder.Property(x => x.UnitNumber)
            .HasMaxLength(50);

        builder.Property(x => x.FleetNumber)
            .HasMaxLength(50);

        builder.Property(x => x.Color)
            .HasMaxLength(60);

        builder.Property(x => x.Memo)
            .HasMaxLength(2000);

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
