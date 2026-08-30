using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Configurations;

public sealed class ServiceHistoryEntryConfiguration
    : IEntityTypeConfiguration<ServiceHistoryEntry>
{
    public void Configure(EntityTypeBuilder<ServiceHistoryEntry> builder)
    {
        builder.ToTable("ServiceHistoryEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.VehicleName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ServiceDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Service)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.WorkOrderNumber)
            .HasMaxLength(50);

        builder.Property(x => x.Complaint)
            .HasMaxLength(4000);

        builder.Property(x => x.Diagnosis)
            .HasMaxLength(4000);

        builder.Property(x => x.Technician)
            .HasMaxLength(120);

        builder.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ApprovalStatus)
            .HasMaxLength(30);

        builder.Property(x => x.EstimateAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.LaborAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.InvoiceNumber)
            .HasMaxLength(50);

        builder.Property(x => x.InvoiceAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.InvoiceStatus)
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue("Draft");

        builder.Property(x => x.InvoiceNotes)
            .HasMaxLength(4000);

        builder.Property(x => x.Notes)
            .HasMaxLength(4000);

        builder.Property(x => x.InternalNotes)
            .HasMaxLength(4000);

        builder.Property(x => x.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CustomerVehicleId);
        builder.HasIndex(x => x.ServiceDate);
        builder.HasIndex(x => x.AppointmentRequestId)
            .IsUnique()
            .HasFilter("[AppointmentRequestId] IS NOT NULL");
        builder.HasIndex(x => new { x.CustomerId, x.ServiceDate });

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.CustomerVehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<AppointmentRequest>()
            .WithMany()
            .HasForeignKey(x => x.AppointmentRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
