using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class ServiceHistoryUpdateConfiguration : IEntityTypeConfiguration<ServiceHistoryUpdate>
{
    public void Configure(EntityTypeBuilder<ServiceHistoryUpdate> builder)
    {
        builder.ToTable("ServiceHistoryUpdates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.AuthorName)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => new { x.ServiceHistoryEntryId, x.CreatedUtc });

        builder.HasOne(x => x.ServiceHistoryEntry)
            .WithMany(x => x.Updates)
            .HasForeignKey(x => x.ServiceHistoryEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
