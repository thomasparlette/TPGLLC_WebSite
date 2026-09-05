using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Integration;
using Xunit;

namespace TPGLLC.Integration.Tests;

public class PortalIntegrationTests
{
    private static TPGLLCDbContext Database() => new(new DbContextOptionsBuilder<TPGLLCDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static RepairSnapshot Snapshot(Guid customer) => new(1, customer, "WO-1", "2020 Ford Focus", "In Progress",
        new DateOnly(2026, 9, 5), "Parts arrived", 120, [new(1, "Filter", 2, 10)]);
    private static async Task<Customer> Customer(TPGLLCDbContext db)
    {
        var customer = new Customer { ApplicationUserId = Guid.NewGuid().ToString(), FirstName = "Test" };
        db.Customers.Add(customer); await db.SaveChangesAsync(); return customer;
    }

    [Fact]
    public async Task RetryDoesNotDuplicateRepairPartsOrTimeline()
    {
        await using var db = Database(); var customer = await Customer(db); var source = Guid.NewGuid();
        var snapshot = Snapshot(customer.Id);
        await AutoShopEndpoints.SaveAsync(db, source, snapshot); db.ChangeTracker.Clear();
        await AutoShopEndpoints.SaveAsync(db, source, snapshot); db.ChangeTracker.Clear();
        Assert.Equal(1, await db.ServiceHistoryEntries.CountAsync());
        Assert.Equal(1, await db.ServiceHistoryParts.CountAsync());
        Assert.Equal(1, await db.ServiceHistoryUpdates.CountAsync());
        var entry = await db.ServiceHistoryEntries.SingleAsync();
        Assert.Null(entry.InternalNotes); Assert.Null(entry.Diagnosis); Assert.Null(entry.InvoiceAmount);
    }

    [Fact]
    public async Task PublishingReplacesPartsAndRecordsChangedStatus()
    {
        await using var db = Database(); var customer = await Customer(db); var source = Guid.NewGuid();
        await AutoShopEndpoints.SaveAsync(db, source, Snapshot(customer.Id)); db.ChangeTracker.Clear();
        await AutoShopEndpoints.SaveAsync(db, source, Snapshot(customer.Id) with { Status = "Completed", Parts = [new(2, "Belt", 1, 30)] });
        Assert.Equal("Belt", (await db.ServiceHistoryParts.SingleAsync()).Description);
        Assert.Equal(2, await db.ServiceHistoryUpdates.CountAsync());
    }

    [Fact]
    public async Task CannotMovePublishedRepairToAnotherCustomer()
    {
        await using var db = Database(); var customer = await Customer(db); var other = await Customer(db); var source = Guid.NewGuid();
        await AutoShopEndpoints.SaveAsync(db, source, Snapshot(customer.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => AutoShopEndpoints.SaveAsync(db, source, Snapshot(other.Id)));
        Assert.Equal(customer.Id, (await db.ServiceHistoryEntries.SingleAsync()).CustomerId);
    }

    [Fact]
    public async Task CannotPublishToUnknownCustomer()
    {
        await using var db = Database();
        await Assert.ThrowsAsync<InvalidOperationException>(() => AutoShopEndpoints.SaveAsync(db, Guid.NewGuid(), Snapshot(Guid.NewGuid())));
        Assert.Empty(db.ServiceHistoryEntries);
    }

    [Fact]
    public void RejectsInvalidPartsAndKeys()
    {
        Assert.NotNull(AutoShopEndpoints.Validate(Snapshot(Guid.NewGuid()) with { Parts = [new(1, "Filter", -1, 20)] }));
        Assert.NotNull(AutoShopEndpoints.Validate(Snapshot(Guid.NewGuid()) with { Parts = [new(1, "Filter", 1, 20), new(1, "Other", 1, 2)] }));
        Assert.NotNull(AutoShopEndpoints.Validate(Snapshot(Guid.NewGuid()) with { Status = "unknown" }));
        Assert.False(AutoShopEndpoints.ValidKey(new string('a', 32), new string('b', 32)));
        Assert.True(AutoShopEndpoints.ValidKey(new string('a', 32), new string('a', 32)));
    }

    [Fact]
    public async Task HttpEndpointRequiresHttpsAndKeyAndPublishesContract()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["AutoShop:ApiKey"] = new string('k', 32), ["AutoShop:SourceId"] = Guid.NewGuid().ToString() });
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContextFactory<TPGLLCDbContext>(o => o.UseInMemoryDatabase(database));
        await using var app = builder.Build(); app.MapAutoShop(); await app.StartAsync();
        using var client = app.GetTestClient(); client.BaseAddress = new Uri("https://localhost");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/autoshop/v1/customers")).StatusCode);
        client.DefaultRequestHeaders.Add("X-AutoShop-Key", new string('k', 32));
        await using var db = await app.Services.GetRequiredService<IDbContextFactory<TPGLLCDbContext>>().CreateDbContextAsync();
        var customer = await Customer(db);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/autoshop/v1/work-orders/1", Snapshot(customer.Id))).StatusCode);
        var publication = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/autoshop/v1/work-orders/1");
        Assert.Equal(customer.Id, publication.GetProperty("customerId").GetGuid());
        Assert.Equal("Parts arrived", publication.GetProperty("customerUpdate").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/autoshop/v1/work-orders/2", Snapshot(customer.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("http://localhost/api/autoshop/v1/customers")).StatusCode);
    }
}
