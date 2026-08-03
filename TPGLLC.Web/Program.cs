using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using System.IO;
using TPGLLC.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataProtection()
    .SetApplicationName("TPGLLC.Web")
    .PersistKeysToFileSystem(
        new DirectoryInfo(@"D:\Websites\TPGLLC\DataProtectionKeys"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

if (builder.Environment.IsEnvironment("Build"))
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

builder.Services.AddTpgllcPlatform(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseTpgllcPipeline();

app.Run();