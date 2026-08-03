using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Services.Vehicles;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Authorization;
using TPGLLC.Web.Services;
using TPGLLC.Web.Services.Appointments;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.Services.Portal;

namespace TPGLLC.Web.Infrastructure;

public static class TpgllcServiceCollectionExtensions
{
    public static IServiceCollection AddTpgllcPlatform(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var configConnection = configuration.GetConnectionString("WebsiteDb");
        var envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb");

        var connectionString =
            configConnection
            ?? envConnection
            ?? throw new InvalidOperationException("Missing WebsiteDb connection string.");

        if (environment.IsEnvironment("Build"))
        {
            services.AddDbContext<TPGLLCDbContext>(options =>
                options.UseInMemoryDatabase("TPGLLC_Build"));

            services.AddDbContextFactory<TPGLLCDbContext>(options =>
                options.UseInMemoryDatabase("TPGLLC_Build"));
        }
        else
        {
            services.AddDbContext<TPGLLCDbContext>(options =>
                options.UseSqlServer(connectionString,
                    sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)));

            services.AddDbContextFactory<TPGLLCDbContext>(options =>
                options.UseSqlServer(connectionString,
                    sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)));
        }

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;

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

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(24);
        });

        var externalAuth = services.AddAuthentication();

        var googleClientId = configuration["Authentication:Google:ClientId"]
            ?? Environment.GetEnvironmentVariable("Authentication__Google__ClientId");
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"]
            ?? Environment.GetEnvironmentVariable("Authentication__Google__ClientSecret");

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            externalAuth.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
            });
        }

        var microsoftClientId = configuration["Authentication:Microsoft:ClientId"]
            ?? Environment.GetEnvironmentVariable("Authentication__Microsoft__ClientId");
        var microsoftClientSecret = configuration["Authentication:Microsoft:ClientSecret"]
            ?? Environment.GetEnvironmentVariable("Authentication__Microsoft__ClientSecret");

        if (!string.IsNullOrWhiteSpace(microsoftClientId) && !string.IsNullOrWhiteSpace(microsoftClientSecret))
        {
            externalAuth.AddMicrosoftAccount(options =>
            {
                options.ClientId = microsoftClientId;
                options.ClientSecret = microsoftClientSecret;
            });
        }

        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.Configure<AppointmentEmailOptions>(
            configuration.GetSection("Gmail"));

        services.AddScoped<IVehicleCatalogService, VehicleCatalogService>();
        services.AddScoped<ICurrentCustomerAccessor, CurrentCustomerAccessor>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IVehiclePortalService, VehiclePortalService>();
        services.AddScoped<IAppointmentPortalService, AppointmentPortalService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IEmailTemplateRenderer, FileEmailTemplateRenderer>();
        services.AddSingleton<IBuildEnvironmentService, BuildEnvironmentService>();
        services.AddScoped<ICustomerAccountService, CustomerAccountService>();
        services.AddScoped<IVehiclePhotoService, VehiclePhotoService>();
        services.AddScoped<IVehicleDetailsService, VehicleDetailsService>();
        services.AddScoped<IPortalContextService, PortalContextService>();

        services.AddAuthorizationBuilder()
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

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.LogoutPath = "/Identity/Account/Logout";

            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.Cookie.Name = "TPGLLC.Identity";
        });

        return services;
    }
}