using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class ServiceHistoryPaymentConfiguration : IEntityTypeConfiguration<ServiceHistoryPayment>
{
    public void Configure(EntityTypeBuilder<ServiceHistoryPayment> builder)
    {
        builder.ToTable("ServiceHistoryPayments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaymentMethod).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ReceivedBy).HasMaxLength(200);
        builder.Property(x => x.ReceivedUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => new { x.ServiceHistoryEntryId, x.ReceivedUtc });
        builder.HasIndex(x => x.PaymentMethod);

        builder.HasOne(x => x.ServiceHistoryEntry)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.ServiceHistoryEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
