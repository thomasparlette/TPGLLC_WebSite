using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class PartsCatalogItemConfiguration : IEntityTypeConfiguration<PartsCatalogItem>
{
    public void Configure(EntityTypeBuilder<PartsCatalogItem> builder)
    {
        builder.ToTable("PartsCatalogItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PartNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.PartNumber).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Name });
    }
}
