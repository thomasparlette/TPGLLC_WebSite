using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public sealed class InvoicePaymentService : IInvoicePaymentService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InvoicePaymentService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbFactory = dbFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<InvoicePaymentPageViewModel> GetCompletedWorkOrdersAsync() =>
        GetPageAsync(InvoicePageKind.CompletedWorkOrders);

    public Task<InvoicePaymentPageViewModel> GetPendingPaymentsAsync() =>
        GetPageAsync(InvoicePageKind.PendingPayments);

    public Task<InvoicePaymentPageViewModel> GetReceivedPaymentsAsync() =>
        GetPageAsync(InvoicePageKind.ReceivedPayments);

    public async Task<InvoicePaymentPageViewModel> IssueInvoiceAsync(Guid workOrderId)
    {
        if (!IsFinanceOrAdministrator())
        {
            return new InvoicePaymentPageViewModel
            {
                ErrorMessage = "You are not authorized to issue invoices."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await db.ServiceHistoryEntries
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == workOrderId);

        if (workOrder is null)
        {
            return await GetCompletedWorkOrdersAsyncWithError("Work order not found.");
        }

        if (!workOrder.InvoiceAmount.HasValue || workOrder.InvoiceAmount.Value <= 0)
        {
            return await GetCompletedWorkOrdersAsyncWithError("The work order must have a final invoice amount before it can be issued.");
        }

        if (string.Equals(workOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(workOrder.Status, "Declined", StringComparison.OrdinalIgnoreCase))
        {
            return await GetCompletedWorkOrdersAsyncWithError("Cancelled or declined work cannot be invoiced.");
        }

        var now = DateTimeOffset.UtcNow;
        workOrder.InvoiceNumber ??= workOrder.WorkOrderNumber ?? $"INV-{workOrder.Id.ToString("N")[..8].ToUpperInvariant()}";
        workOrder.InvoiceIssuedUtc ??= now;
        workOrder.InvoiceDueUtc ??= now.AddDays(30);
        workOrder.InvoiceStatus = CalculateInvoiceStatus(workOrder, now, issued: true);
        workOrder.Status = "Invoiced";
        workOrder.UpdatedUtc = now;

        await db.SaveChangesAsync();

        var result = await GetCompletedWorkOrdersAsync();
        result.SuccessMessage = $"Invoice {workOrder.InvoiceNumber} issued.";
        return result;
    }

    public async Task<InvoicePaymentPageViewModel> RecordPaymentAsync(InvoicePaymentPageViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!IsFinanceOrAdministrator())
        {
            model.ErrorMessage = "You are not authorized to record payments.";
            return model;
        }

        if (!PaymentMethodCatalog.IsAccepted(model.PaymentForm.PaymentMethod))
        {
            model.ErrorMessage = "Payment method must be Cash, PayPal, or Venmo.";
            return model;
        }

        if (!TryParseMoney(model.PaymentForm.Amount, out var amount) || amount <= 0)
        {
            model.ErrorMessage = "Payment amount must be greater than zero.";
            return model;
        }

        var paymentAmount = amount.GetValueOrDefault();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var workOrder = await db.ServiceHistoryEntries
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == model.PaymentForm.WorkOrderId);

        if (workOrder is null)
        {
            model.ErrorMessage = "Invoice work order not found.";
            return model;
        }

        if (!workOrder.InvoiceAmount.HasValue || workOrder.InvoiceAmount.Value <= 0)
        {
            model.ErrorMessage = "This work order does not have an invoice amount.";
            return model;
        }

        if (string.Equals(workOrder.InvoiceStatus, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(workOrder.InvoiceStatus, "Void", StringComparison.OrdinalIgnoreCase))
        {
            model.ErrorMessage = "Issue the invoice before recording a payment.";
            return model;
        }

        var paidBefore = workOrder.Payments.Sum(x => x.Amount);
        var balanceBefore = Math.Max(0m, workOrder.InvoiceAmount.Value - paidBefore);
        if (paymentAmount > balanceBefore + 0.01m)
        {
            model.ErrorMessage = $"Payment cannot exceed the remaining balance of {balanceBefore.ToString("C", CultureInfo.CurrentCulture)}.";
            return model;
        }

        var receivedDate = model.PaymentForm.ReceivedDate == default
            ? DateTime.Today
            : model.PaymentForm.ReceivedDate.Date;
        var receivedUtc = new DateTimeOffset(receivedDate, TimeSpan.Zero);

        db.ServiceHistoryPayments.Add(new ServiceHistoryPayment
        {
            ServiceHistoryEntryId = workOrder.Id,
            Amount = paymentAmount,
            PaymentMethod = model.PaymentForm.PaymentMethod.Trim(),
            ReferenceNumber = Normalize(model.PaymentForm.ReferenceNumber),
            Notes = Normalize(model.PaymentForm.Notes),
            ReceivedUtc = receivedUtc,
            ReceivedBy = GetCurrentUserName()
        });

        var balanceAfter = Math.Max(0m, balanceBefore - paymentAmount);
        workOrder.InvoiceStatus = balanceAfter <= 0.01m ? "Paid" : "Partially Paid";
        workOrder.Status = balanceAfter <= 0.01m ? "Closed" : "Invoiced";
        workOrder.UpdatedUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        var result = await GetPendingPaymentsAsync();
        result.SuccessMessage = balanceAfter <= 0.01m
            ? $"Payment recorded. Invoice {workOrder.InvoiceNumber ?? workOrder.WorkOrderNumber} is paid in full."
            : "Payment recorded and invoice balance updated.";
        return result;
    }

    private async Task<InvoicePaymentPageViewModel> GetPageAsync(InvoicePageKind pageKind)
    {
        if (!IsFinanceOrAdministrator())
        {
            return new InvoicePaymentPageViewModel
            {
                ErrorMessage = "You are not authorized to view invoice and payment records."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var entries = await db.ServiceHistoryEntries
            .Include(x => x.Customer)
            .Include(x => x.Payments)
            .Where(x => x.InvoiceAmount.HasValue && x.InvoiceAmount.Value > 0)
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        var changed = false;
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in entries)
        {
            var status = CalculateInvoiceStatus(entry, now, issued: entry.InvoiceIssuedUtc.HasValue);
            if (!string.Equals(entry.InvoiceStatus, status, StringComparison.OrdinalIgnoreCase))
            {
                entry.InvoiceStatus = status;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }

        var invoices = entries.Select(MapInvoice).Where(invoice => pageKind switch
        {
            InvoicePageKind.CompletedWorkOrders => string.Equals(invoice.InvoiceStatus, "Draft", StringComparison.OrdinalIgnoreCase)
                && entries.First(x => x.Id == invoice.Id).Status.Equals("Completed", StringComparison.OrdinalIgnoreCase),
            InvoicePageKind.PendingPayments => !string.Equals(invoice.InvoiceStatus, "Draft", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(invoice.InvoiceStatus, "Void", StringComparison.OrdinalIgnoreCase)
                && invoice.BalanceDue > 0.01m,
            InvoicePageKind.ReceivedPayments => invoice.Payments.Count > 0,
            _ => false
        }).ToList();

        return new InvoicePaymentPageViewModel
        {
            Invoices = invoices
        };
    }

    private async Task<InvoicePaymentPageViewModel> GetCompletedWorkOrdersAsyncWithError(string message)
    {
        var result = await GetCompletedWorkOrdersAsync();
        result.ErrorMessage = message;
        return result;
    }

    private static InvoiceSummaryViewModel MapInvoice(ServiceHistoryEntry entry)
    {
        var payments = entry.Payments
            .OrderByDescending(x => x.ReceivedUtc)
            .Select(x => new PaymentSummaryViewModel
            {
                Id = x.Id,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                ReferenceNumber = x.ReferenceNumber,
                Notes = x.Notes,
                ReceivedUtc = x.ReceivedUtc,
                ReceivedBy = x.ReceivedBy
            })
            .ToList();

        return new InvoiceSummaryViewModel
        {
            Id = entry.Id,
            CustomerName = string.Join(" ", new[] { entry.Customer?.FirstName, entry.Customer?.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))).Trim() is { Length: > 0 } name ? name : "Customer",
            CustomerEmail = entry.Customer?.Email,
            VehicleName = entry.VehicleName,
            Service = entry.Service,
            ServiceDate = entry.ServiceDate,
            WorkOrderNumber = entry.WorkOrderNumber,
            InvoiceNumber = entry.InvoiceNumber ?? entry.WorkOrderNumber,
            InvoiceStatus = InvoiceStatusCatalog.GetLabel(entry.InvoiceStatus),
            InvoiceIssuedUtc = entry.InvoiceIssuedUtc,
            InvoiceDueUtc = entry.InvoiceDueUtc,
            InvoiceTotal = entry.InvoiceAmount ?? 0m,
            PaidAmount = payments.Sum(x => x.Amount),
            Payments = payments
        };
    }

    private static string CalculateInvoiceStatus(
        ServiceHistoryEntry entry,
        DateTimeOffset now,
        bool issued)
    {
        if (string.Equals(entry.InvoiceStatus, "Void", StringComparison.OrdinalIgnoreCase))
        {
            return "Void";
        }

        if (!issued)
        {
            return "Draft";
        }

        var total = entry.InvoiceAmount ?? 0m;
        var paid = entry.Payments.Sum(x => x.Amount);
        if (paid >= total - 0.01m)
        {
            return "Paid";
        }

        if (entry.InvoiceDueUtc.HasValue && entry.InvoiceDueUtc.Value < now)
        {
            return "Overdue";
        }

        return paid > 0.01m ? "Partially Paid" : "Sent";
    }

    private bool IsFinanceOrAdministrator()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole("Finance") == true || user?.IsInRole("Administrator") == true;
    }

    private string? GetCurrentUserName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var name = user?.Identity?.Name;
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private static bool TryParseMoney(string? value, out decimal? result)
    {
        result = null;
        var normalized = value?.Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0)
        {
            return false;
        }

        result = parsed;
        return true;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private enum InvoicePageKind
    {
        CompletedWorkOrders,
        PendingPayments,
        ReceivedPayments
    }
}
