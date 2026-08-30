using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TPGLLC.Data;
using TPGLLC.Tools.VehicleImporter;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile(
    "VehicleImportSettings.json",
    optional: false,
    reloadOnChange: false);

var configurationRoot = (IConfigurationRoot)builder.Configuration;

Console.WriteLine("========== Configuration ==========");
Console.WriteLine($"Environment : {builder.Environment.EnvironmentName}");
Console.WriteLine($"ContentRoot : {builder.Environment.ContentRootPath}");
Console.WriteLine($"BaseDirectory : {AppContext.BaseDirectory}");
Console.WriteLine();

Console.WriteLine("Providers:");

foreach (var provider in configurationRoot.Providers)
{
    Console.WriteLine($" - {provider}");
}

Console.WriteLine();

var connectionString =
    builder.Configuration.GetConnectionString("WebsiteDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb");

Console.WriteLine($"WebsiteDb : {connectionString ?? "<null>"}");
Console.WriteLine("===================================");
Console.WriteLine();

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing WebsiteDb connection string.");
}

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

builder.Services.AddDbContext<TPGLLCDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sql =>
        {
            sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName);
            sql.EnableRetryOnFailure();
        });
});

builder.Services.AddHttpClient<IVpicApiClient, VpicApiClient>(client =>
{
    client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/");
    client.Timeout = TimeSpan.FromMinutes(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TPGLLC.VehicleImporter/1.0");
});

builder.Services.Configure<VehicleImportSettings>(builder.Configuration);

builder.Services.AddScoped<VehicleCatalogImportService>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting Vehicle Catalog Import.");

var importer =
    scope.ServiceProvider.GetRequiredService<VehicleCatalogImportService>();

await importer.RunAsync();

logger.LogInformation("Vehicle Catalog Import Complete.");
