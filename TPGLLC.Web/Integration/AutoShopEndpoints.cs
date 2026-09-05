using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;

namespace TPGLLC.Web.Integration;

public static class AutoShopEndpoints
{
    public static void MapAutoShop(this WebApplication app)
    {
        var group = app.MapGroup("/api/autoshop/v1");
        group.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var expected = app.Configuration["AutoShop:ApiKey"];
            var supplied = http.Request.Headers["X-AutoShop-Key"].ToString();
            if (!http.Request.IsHttps) return Results.BadRequest("HTTPS is required.");
            if (string.IsNullOrWhiteSpace(expected) || expected.Length < 32
                || supplied.Length > 512 || !ValidKey(expected, supplied))
                return Results.Unauthorized();
            if (!Guid.TryParse(app.Configuration["AutoShop:SourceId"], out var source) || source == Guid.Empty)
                return Results.Problem("AutoShop source is not configured.");
            if (http.Request.ContentLength > 131072) return Results.StatusCode(413);
            return await next(context);
        });

        group.MapGet("/customers", async (IDbContextFactory<TPGLLCDbContext> factory, CancellationToken ct) =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            return Results.Ok(await db.Customers.AsNoTracking()
                .Where(c => c.ApplicationUserId != null)
                .OrderBy(c => c.LastName)
                .Select(c => new { c.Id, Name = c.FirstName + " " + c.LastName, c.Email })
                .ToListAsync(ct));
        });

        group.MapGet("/work-orders/{id:int}", async (int id, IDbContextFactory<TPGLLCDbContext> factory, CancellationToken ct) =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var source = Guid.Parse(app.Configuration["AutoShop:SourceId"]!);
            var key = StableId(source, $"work-order:{id}");
            var entry = await db.ServiceHistoryEntries.AsNoTracking().Where(e => e.Id == key)
                .Select(e => new { e.CustomerId, CustomerUpdate = e.Notes, e.UpdatedUtc }).SingleOrDefaultAsync(ct);
            return entry == null ? Results.NotFound() : Results.Ok(entry);
        });

        group.MapPut("/work-orders/{id:int}", async (int id, RepairSnapshot snapshot,
            IDbContextFactory<TPGLLCDbContext> factory, CancellationToken ct) =>
        {
            var error = Validate(snapshot);
            if (id != snapshot.WorkOrderId || error != null)
                return Results.BadRequest(error ?? "Work order ID does not match.");
            await using var db = await factory.CreateDbContextAsync(ct);
            var source = Guid.Parse(app.Configuration["AutoShop:SourceId"]!);
            try
            {
                await SaveAsync(db, source, snapshot, ct);
                return Results.Ok(new { Message = "Repair update published to the customer portal." });
            }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            catch (DbUpdateException) { return Results.Conflict("The repair changed during publishing. Please retry."); }
        });
    }

    public static bool ValidKey(string expected, string supplied) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied)));

    public static string? Validate(RepairSnapshot s)
    {
        if (s.WorkOrderId <= 0 || s.CustomerId == Guid.Empty) return "A work order and linked customer are required.";
        if (string.IsNullOrWhiteSpace(s.Number) || s.Number.Length > 50
            || string.IsNullOrWhiteSpace(s.Vehicle) || s.Vehicle.Length > 200) return "Invalid work order number or vehicle.";
        if (s.Status is not ("Requested" or "In Progress" or "Waiting on Customer Approval" or "Completed" or "Closed" or "Cancelled"))
            return "Unsupported repair status.";
        if (s.CustomerUpdate?.Length > 4000 || s.Estimate < 0 || s.Estimate > 999999999m) return "Invalid update or estimate.";
        if (s.Parts == null || s.Parts.Count > 200 || s.Parts.Any(p => p == null || p.Id <= 0
            || string.IsNullOrWhiteSpace(p.Description) || p.Description.Length > 250
            || p.Quantity <= 0 || p.Quantity > 999999m || p.UnitPrice < 0 || p.UnitPrice > 999999999m)
            || s.Parts.Select(p => p.Id).Distinct().Count() != s.Parts.Count) return "Invalid parts.";
        return null;
    }

    public static async Task SaveAsync(TPGLLCDbContext db, Guid source, RepairSnapshot s, CancellationToken ct = default)
    {
        if (Validate(s) is { } error) throw new InvalidOperationException(error);
        if (source == Guid.Empty) throw new InvalidOperationException("A stable shop source ID is required.");
        if (!await db.Customers.AnyAsync(c => c.Id == s.CustomerId && c.ApplicationUserId != null, ct))
            throw new InvalidOperationException("Select a customer with an existing portal account.");
        var id = StableId(source, $"work-order:{s.WorkOrderId}");
        var entry = await db.ServiceHistoryEntries.Include(e => e.Parts).SingleOrDefaultAsync(e => e.Id == id, ct);
        if (entry != null && entry.CustomerId != s.CustomerId)
            throw new InvalidOperationException("This work order is already linked to another customer. Contact the administrator.");
        var isNew = entry == null;
        entry ??= new ServiceHistoryEntry { Id = id, CustomerId = s.CustomerId };
        var changed = isNew || entry.Status != s.Status || entry.Notes != s.CustomerUpdate
            || entry.EstimateAmount != s.Estimate || entry.VehicleName != s.Vehicle || entry.WorkOrderNumber != s.Number
            || entry.Parts.Count != s.Parts.Count || s.Parts.Any(p => !entry.Parts.Any(old =>
                old.Id == StableId(source, $"work-order:{s.WorkOrderId}:part:{p.Id}")
                && old.Description == p.Description && old.Quantity == p.Quantity && old.UnitPrice == p.UnitPrice));
        entry.Service = "Repair managed by AutoShop";
        entry.WorkOrderNumber = s.Number;
        entry.VehicleName = s.Vehicle;
        entry.ServiceDate = s.ServiceDate;
        entry.Status = s.Status;
        entry.Notes = s.CustomerUpdate;
        entry.EstimateAmount = s.Estimate;
        // Imported repairs are tracking-only; estimates and approvals remain in the shop.
        entry.ApprovalStatus = "Contact shop";
        var incoming = s.Parts.Select(p => StableId(source, $"work-order:{s.WorkOrderId}:part:{p.Id}")).ToHashSet();
        db.ServiceHistoryParts.RemoveRange(entry.Parts.Where(p => !incoming.Contains(p.Id)));
        foreach (var p in s.Parts)
        {
            var partId = StableId(source, $"work-order:{s.WorkOrderId}:part:{p.Id}");
            var part = entry.Parts.SingleOrDefault(x => x.Id == partId);
            if (part == null)
            {
                part = new ServiceHistoryPart { Id = partId, ServiceHistoryEntryId = entry.Id };
                entry.Parts.Add(part);
                db.ServiceHistoryParts.Add(part);
            }
            part.Description = p.Description;
            part.Quantity = p.Quantity;
            part.UnitPrice = p.UnitPrice;
        }
        if (changed)
        {
            entry.UpdatedUtc = DateTimeOffset.UtcNow;
            var update = new ServiceHistoryUpdate
            {
                ServiceHistoryEntryId = entry.Id,
                Status = s.Status, Message = string.IsNullOrWhiteSpace(s.CustomerUpdate) ? $"Repair status: {s.Status}" : s.CustomerUpdate,
                AuthorName = "Repair team", IsCustomerVisible = true
            };
            entry.Updates.Add(update);
            db.ServiceHistoryUpdates.Add(update);
        }
        if (isNew) db.ServiceHistoryEntries.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    private static Guid StableId(Guid source, string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"{source:D}:{key}"))[..16]);
}
