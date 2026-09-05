using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using TPGLLC.Data;
using TPGLLC.Services.Vehicles;
using Xunit;

namespace TPGLLC.Integration.Tests;

public class VehicleCatalogFallbackTests
{
    [Fact]
    public async Task MissingOptionalTableReturnsFallbackAndCachesBriefly()
    {
        var failure = new QueryFailure(CreateSqlException(208));
        await using var db = Database(failure);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var catalog = new VehicleCatalogService(db, cache);
        Assert.Empty(await catalog.GetOptionsAsync("BodyStyle"));
        Assert.Empty(await catalog.GetOptionsAsync("BodyStyle"));
        Assert.Equal(1, failure.Calls);
    }

    [Theory]
    [InlineData(18456)] // Login failure must not be mistaken for an optional table.
    [InlineData(207)]   // Invalid columns need a real schema fix.
    public async Task OtherDatabaseFailuresPropagate(int number)
    {
        await using var db = Database(new QueryFailure(CreateSqlException(number)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await Assert.ThrowsAsync<SqlException>(() => new VehicleCatalogService(db, cache).GetOptionsAsync("BodyStyle"));
    }

    [Fact]
    public async Task ExistingCatalogStillReturnsDatabaseChoices()
    {
        await using var db = Database();
        db.VehicleCatalogOptions.Add(new() { Category = "BodyStyle", Value = "Hatchback", Source = "Test" });
        await db.SaveChangesAsync();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        Assert.Equal(new[] { "Hatchback" }, await new VehicleCatalogService(db, cache).GetOptionsAsync("BodyStyle"));
    }

    private static TPGLLCDbContext Database(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<TPGLLCDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptors).Options);

    private sealed class QueryFailure(Exception error) : IQueryExpressionInterceptor
    {
        public int Calls { get; private set; }
        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
        { Calls++; throw error; }
    }

    // SqlClient has no public exception constructor. Manufacture its actual error
    // type for provider-failure simulation without contacting any SQL Server.
    private static SqlException CreateSqlException(int number)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
        var ctor = typeof(SqlError).GetConstructors(flags).First(c => c.GetParameters().Length == 9);
        var error = (SqlError)ctor.Invoke(new object?[] { number, (byte)1, (byte)16, "test", "Simulated SQL error", "test", 1, 0, null });
        typeof(SqlErrorCollection).GetMethod("Add", flags)!.Invoke(errors, [error]);
        var factory = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
            null, [typeof(SqlErrorCollection), typeof(string)], null)!;
        return (SqlException)factory.Invoke(null, [errors, "test"])!;
    }
}
