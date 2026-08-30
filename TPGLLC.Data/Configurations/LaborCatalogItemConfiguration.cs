using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class LaborCatalogItemConfiguration : IEntityTypeConfiguration<LaborCatalogItem>
{
    public void Configure(EntityTypeBuilder<LaborCatalogItem> builder)
    {
        builder.ToTable("LaborCatalogItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DefaultHours).HasColumnType("decimal(18,2)");
        builder.Property(x => x.HourlyRate).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Name });
    }
}
