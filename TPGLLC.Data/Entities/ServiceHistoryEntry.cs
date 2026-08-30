namespace TPGLLC.Data.Entities;

public sealed class ServiceHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public Guid? CustomerVehicleId { get; set; }
    public Guid? AppointmentRequestId { get; set; }

    public string VehicleName { get; set; } = string.Empty;
    public DateOnly ServiceDate { get; set; }
    public string Service { get; set; } = string.Empty;

    public string? WorkOrderNumber { get; set; }
    public string? Complaint { get; set; }
    public string? Diagnosis { get; set; }

    public int? Mileage { get; set; }
    public int? MileageOut { get; set; }
    public string? Technician { get; set; }

    public string Status { get; set; } = "Completed";
    public string? ApprovalStatus { get; set; }
    public decimal? EstimateAmount { get; set; }
    public decimal? LaborAmount { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal? InvoiceAmount { get; set; }
    public string InvoiceStatus { get; set; } = "Draft";
    public DateTimeOffset? InvoiceIssuedUtc { get; set; }
    public DateTimeOffset? InvoiceDueUtc { get; set; }
    public string? InvoiceNotes { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }

    public Customer? Customer { get; set; }
    public CustomerVehicle? Vehicle { get; set; }
    public ICollection<ServiceHistoryPart> Parts { get; set; } = [];
    public ICollection<ServiceHistoryJob> Jobs { get; set; } = [];
    public ICollection<ServiceHistoryInspection> Inspections { get; set; } = [];
    public ICollection<ServiceHistoryUpdate> Updates { get; set; } = [];
    public ICollection<ServiceHistoryPayment> Payments { get; set; } = [];
}
