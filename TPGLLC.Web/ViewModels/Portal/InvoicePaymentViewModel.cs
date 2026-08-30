using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.ViewModels.Portal;

public static class PaymentMethodCatalog
{
    public static IReadOnlyList<string> AcceptedMethods { get; } = ["Cash", "PayPal", "Venmo"];

    public static bool IsAccepted(string? method) =>
        AcceptedMethods.Contains(method?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
}

public static class InvoiceStatusCatalog
{
    public static string GetLabel(string? status) => status?.Trim() switch
    {
        "Sent" => "Sent",
        "Partially Paid" => "Partially Paid",
        "Paid" => "Paid",
        "Overdue" => "Overdue",
        "Void" => "Void",
        _ => "Draft"
    };
}

public sealed class InvoicePaymentPageViewModel
{
    public List<InvoiceSummaryViewModel> Invoices { get; set; } = [];
    public PaymentEntryViewModel PaymentForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public sealed class InvoiceSummaryViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateOnly ServiceDate { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public string InvoiceStatus { get; set; } = "Draft";
    public DateTimeOffset? InvoiceIssuedUtc { get; set; }
    public DateTimeOffset? InvoiceDueUtc { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue => Math.Max(0m, InvoiceTotal - PaidAmount);
    public List<PaymentSummaryViewModel> Payments { get; set; } = [];
}

public sealed class PaymentSummaryViewModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset ReceivedUtc { get; set; }
    public string? ReceivedBy { get; set; }
}

public sealed class PaymentEntryViewModel
{
    public Guid WorkOrderId { get; set; }

    [Required]
    [Display(Name = "Payment amount")]
    public string Amount { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = "Cash";

    [DataType(DataType.Date)]
    [Display(Name = "Received date")]
    public DateTime ReceivedDate { get; set; } = DateTime.Today;

    [StringLength(100)]
    [Display(Name = "Reference number")]
    public string? ReferenceNumber { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
