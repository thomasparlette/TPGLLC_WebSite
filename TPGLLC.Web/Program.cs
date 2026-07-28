using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Components;
using TPGLLC.Web.Services;
using TPGLLC.Web.Features.Portal;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

var configurationRoot = (IConfigurationRoot)builder.Configuration;

Console.WriteLine("========== Configuration ==========");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ContentRoot: {builder.Environment.ContentRootPath}");
Console.WriteLine();

Console.WriteLine("Providers:");
foreach (var provider in configurationRoot.Providers)
{
    Console.WriteLine($" - {provider}");
}

Console.WriteLine();

var configConnection = builder.Configuration.GetConnectionString("WebsiteDb");
var envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb");

Console.WriteLine($"Config Connection : {configConnection ?? "<null>"}");
Console.WriteLine($"Env Connection    : {envConnection ?? "<null>"}");
Console.WriteLine("===================================");

var connectionString =
    configConnection
    ?? envConnection
    ?? throw new InvalidOperationException("Missing WebsiteDb connection string.");

/*var connectionString =
    builder.Configuration.GetConnectionString("WebsiteDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb")
    ?? throw new InvalidOperationException("Missing WebsiteDb connection string.");
*/
builder.Services.AddDbContext<TPGLLCDbContext>(options =>
    options.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)));

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<TPGLLCDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/Login";
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

var apiBaseUrl =
    builder.Configuration["Api:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("Api__BaseUrl")
    ?? throw new InvalidOperationException("Missing Api:BaseUrl configuration.");

if (builder.Environment.IsProduction() &&
    Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var parsedBaseUrl) &&
    parsedBaseUrl.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase))
{
    apiBaseUrl = "https://api.tomparlettegarage.org/";
}

builder.Services.AddHttpClient<VehicleApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<CustomerPortalStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapGet("/health", () => Results.Text("OK", "text/plain"));
app.MapGet("/version", (IWebHostEnvironment env) =>
{
    var versionPath = Path.Combine(env.ContentRootPath, "version.json");
    return File.Exists(versionPath)
        ? Results.File(versionPath, "application/json")
        : Results.NotFound();
});
app.UseHttpsRedirection();
app.MapControllers();
app.Run();