using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TPGLLC.Data;
using TPGLLC.Tools.VehicleImporter;

// Ensure the working directory is the executable folder so appsettings.json
// and appsettings.Development.json are loaded correctly.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile(
    "VehicleImportSettings.json",
    optional: false,
    reloadOnChange: false);

// ---------------------------------------------------------------------
// Diagnostics (remove later if desired)
// ---------------------------------------------------------------------
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
    throw new InvalidOperationException(
        "Missing WebsiteDb connection string.");
}

// ---------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

// ---------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------
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

// ---------------------------------------------------------------------
// HTTP Client
// ---------------------------------------------------------------------
builder.Services.AddHttpClient<IVpicApiClient, VpicApiClient>(client =>
{
    client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/");
    client.Timeout = TimeSpan.FromMinutes(10);
});

// ---------------------------------------------------------------------
// Import Services
// ---------------------------------------------------------------------
builder.Services.Configure<VehicleImportSettings>(
    builder.Configuration.GetSection(VehicleImportSettings.SectionName));

builder.Services.AddScoped<VehicleCatalogImportService>();

// ---------------------------------------------------------------------
// Run Import
// ---------------------------------------------------------------------
using var host = builder.Build();

using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var settings = scope.ServiceProvider.GetRequiredService<IOptions<VehicleImportSettings>>().Value;

logger.LogInformation(
    "Starting Vehicle Catalog Import for years {StartYear}-{EndYear} with {AllowedCount} allowed makes.",
    settings.StartYear,
    settings.EndYear,
    settings.AllowedMakes.Count);

var importer =
    scope.ServiceProvider.GetRequiredService<VehicleCatalogImportService>();

await importer.RunAsync();

logger.LogInformation("Vehicle Catalog Import Complete.");
