using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class ServiceHistoryInspectionConfiguration : IEntityTypeConfiguration<ServiceHistoryInspection>
{
    public void Configure(EntityTypeBuilder<ServiceHistoryInspection> builder)
    {
        builder.ToTable("ServiceHistoryInspections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Area).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Condition).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Finding).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Recommendation).HasMaxLength(4000);
        builder.Property(x => x.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.ServiceHistoryEntryId, x.CreatedUtc });
        builder.HasIndex(x => x.Condition);

        builder.HasOne(x => x.ServiceHistoryEntry)
            .WithMany(x => x.Inspections)
            .HasForeignKey(x => x.ServiceHistoryEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
