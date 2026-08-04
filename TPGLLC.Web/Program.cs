using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using System.IO;
using TPGLLC.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var keyPath = builder.Configuration["DataProtection:KeyPath"]
    ?? throw new InvalidOperationException(
        "Missing DataProtection:KeyPath configuration.");

Directory.CreateDirectory(keyPath);

builder.Services
    .AddDataProtection()
    .SetApplicationName("TPGLLC.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

if (builder.Environment.IsEnvironment("Build"))
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

builder.Services.AddTpgllcPlatform(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsEnvironment("Build"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<BuildEnvironmentSeeder>();
    await seeder.SeedAsync();
}

app.UseTpgllcPipeline();

app.Run();