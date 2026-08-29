using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class ServiceHistoryJobConfiguration : IEntityTypeConfiguration<ServiceHistoryJob>
{
    public void Configure(EntityTypeBuilder<ServiceHistoryJob> builder)
    {
        builder.ToTable("ServiceHistoryJobs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.LaborAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.ServiceHistoryEntryId, x.SortOrder });
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.ServiceHistoryEntry)
            .WithMany(x => x.Jobs)
            .HasForeignKey(x => x.ServiceHistoryEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
