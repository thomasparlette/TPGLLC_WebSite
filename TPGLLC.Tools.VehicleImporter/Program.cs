using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TPGLLC.Data;
using TPGLLC.Tools.VehicleImporter;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

builder.Services.AddDbContext<TPGLLCDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("WebsiteDb"),
        sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)));

builder.Services.AddHttpClient<IVpicApiClient, VpicApiClient>(client =>
{
    client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/");
    client.Timeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddScoped<VehicleCatalogImportService>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var importer = scope.ServiceProvider.GetRequiredService<VehicleCatalogImportService>();
await importer.RunAsync(CancellationToken.None);