using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Components;
using TPGLLC.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

var connectionString =
    builder.Configuration.GetConnectionString("WebsiteDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb")
    ?? throw new InvalidOperationException("Missing WebsiteDb connection string.");

builder.Services.AddDbContext<TPGLLCDbContext>(options =>
    options.UseSqlServer(
        connectionString,
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
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/login";
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

var apiBaseUrl =
    builder.Configuration["Api:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("Api__BaseUrl")
    ?? throw new InvalidOperationException("Missing Api:BaseUrl configuration.");

builder.Services.AddHttpClient<VehicleApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(60);
});

/*
builder.Services.AddHttpClient<AppointmentApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(60);
});
*/

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

app.Run();