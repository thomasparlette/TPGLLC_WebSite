using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class VehicleCatalogOptionConfiguration
    : IEntityTypeConfiguration<VehicleCatalogOption>
{
    public void Configure(EntityTypeBuilder<VehicleCatalogOption> builder)
    {
        builder.ToTable("VehicleCatalogOptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => new { x.Category, x.Value })
            .IsUnique();

        builder.HasIndex(x => x.Category);
    }
}
