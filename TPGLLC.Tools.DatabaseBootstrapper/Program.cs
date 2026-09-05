using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TPGLLC.Data;
using TPGLLC.Shared.Identity;
using TPGLLC.Tools.DatabaseBootstrapper;

var builder = Host.CreateApplicationBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

Console.WriteLine("===================================");
Console.WriteLine("TPGLLC Database Bootstrapper");
Console.WriteLine($"Environment : {environmentName}");
Console.WriteLine($"Content Root: {builder.Environment.ContentRootPath}");
Console.WriteLine("===================================");

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(
        "appsettings.json",
        optional: false,
        reloadOnChange: false)
    .AddJsonFile(
        $"appsettings.{environmentName}.json",
        optional: true,
        reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.Configure<BootstrapOptions>(
    builder.Configuration.GetSection("Bootstrap"));

var connectionString =
    builder.Configuration.GetConnectionString("WebsiteDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Missing connection string 'WebsiteDb' for environment '{environmentName}'. " +
        $"Expected appsettings.{environmentName}.json or the " +
        "'ConnectionStrings__WebsiteDb' environment variable.");
}

Console.WriteLine($"Database    : {GetDatabaseName(connectionString)}");
Console.WriteLine();

builder.Services.AddDbContext<TPGLLCDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sql =>
        {
            sql.MigrationsAssembly(
                typeof(TPGLLCDbContext).Assembly.FullName);

            sql.EnableRetryOnFailure();
        });
});

builder.Services.AddDataProtection();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<TPGLLCDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<BootstrapRunner>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var runner =
    scope.ServiceProvider.GetRequiredService<BootstrapRunner>();

await runner.RunAsync();

return 0;

static string GetDatabaseName(string connectionString)
{
    var builder =
        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
            connectionString);

    return builder.InitialCatalog;
}