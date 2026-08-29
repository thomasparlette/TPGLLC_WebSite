using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class ServiceHistoryPartConfiguration : IEntityTypeConfiguration<ServiceHistoryPart>
{
    public void Configure(EntityTypeBuilder<ServiceHistoryPart> builder)
    {
        builder.ToTable("ServiceHistoryParts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.ServiceHistoryEntryId);
        builder.HasIndex(x => x.ServiceHistoryJobId);
        builder.HasOne(x => x.ServiceHistoryEntry).WithMany(x => x.Parts)
            .HasForeignKey(x => x.ServiceHistoryEntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ServiceHistoryJob).WithMany(x => x.Parts)
            .HasForeignKey(x => x.ServiceHistoryJobId).OnDelete(DeleteBehavior.NoAction);
    }
}
