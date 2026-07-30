using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Authorization;
using TPGLLC.Web.Components;
using TPGLLC.Web.Features.Portal;
using TPGLLC.Web.Services.Appointments;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Services.Vehicles;
using TPGLLC.Web.Services;
using System.IO;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .SetApplicationName("TPGLLC.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(@"D:\TPGLLC\DataProtectionKeys"));

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

builder.Services.AddDbContext<TPGLLCDbContext>(options =>
    options.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)));

builder.Services.AddDbContextFactory<TPGLLCDbContext>(options =>
    options.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)));

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.SignIn.RequireConfirmedAccount = true;

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

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});

var externalAuth = builder.Services.AddAuthentication();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"]
    ?? Environment.GetEnvironmentVariable("Authentication__Google__ClientId");
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
    ?? Environment.GetEnvironmentVariable("Authentication__Google__ClientSecret");
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    externalAuth.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"]
    ?? Environment.GetEnvironmentVariable("Authentication__Microsoft__ClientId");
var microsoftClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]
    ?? Environment.GetEnvironmentVariable("Authentication__Microsoft__ClientSecret");
if (!string.IsNullOrWhiteSpace(microsoftClientId) && !string.IsNullOrWhiteSpace(microsoftClientSecret))
{
    externalAuth.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = microsoftClientSecret;
    });
}

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<CustomerPortalStore>();
builder.Services.AddScoped<IVehicleCatalogService, VehicleCatalogService>();

builder.Services.Configure<AppointmentEmailOptions>(
    builder.Configuration.GetSection("Gmail"));

builder.Services.AddScoped<IEmailTemplateRenderer, FileEmailTemplateRenderer>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

builder.Services.AddScoped<ICurrentCustomerAccessor, CurrentCustomerAccessor>();
builder.Services.AddScoped<ICustomerProfileService, CustomerProfileService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();


builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PortalPolicies.Customer, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Customer");
    })
    .AddPolicy(PortalPolicies.Employee, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Employee");
    })
    .AddPolicy(PortalPolicies.Administrator, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator");
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";

    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.Cookie.Name = "TPGLLC.Identity";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapStaticAssets();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapGet("/health", () => Results.Text("OK", "text/plain"))
   .AllowAnonymous();

app.MapGet("/version", (IWebHostEnvironment env) =>
{
    var versionPath = Path.Combine(env.ContentRootPath, "version.json");
    return File.Exists(versionPath)
        ? Results.File(versionPath, "application/json")
        : Results.NotFound();
})
.AllowAnonymous();

app.Run();
