namespace TPGLLC.Data.Entities;

public sealed class ServiceHistoryPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceHistoryEntryId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset ReceivedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ReceivedBy { get; set; }

    public ServiceHistoryEntry? ServiceHistoryEntry { get; set; }
}
