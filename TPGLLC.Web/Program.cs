using Microsoft.AspNetCore.DataProtection;
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

builder.Services.AddTpgllcPlatform(builder.Configuration);
builder.WebHost.UseStaticWebAssets();
var app = builder.Build();

app.UseTpgllcPipeline();

app.Run();