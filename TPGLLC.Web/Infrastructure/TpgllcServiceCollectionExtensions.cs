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
using TPGLLC.Web.Services.Vehicles;
using TPGLLC.Web.Services.WorkOrders;

namespace TPGLLC.Web.Infrastructure;

public static class TpgllcServiceCollectionExtensions
{
    public static IServiceCollection AddTpgllcPlatform(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configConnection = configuration.GetConnectionString("WebsiteDb");
        var envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb");

        var connectionString =
            configConnection
            ?? envConnection
            ?? throw new InvalidOperationException("Missing WebsiteDb connection string.");

        services.AddDbContext<TPGLLCDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)));

        services.AddDbContextFactory<TPGLLCDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(TPGLLCDbContext).Assembly.FullName)),
            ServiceLifetime.Scoped);

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

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TPGLLCUserClaimsPrincipalFactory>();

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
        services.AddScoped<IPortalSessionState, PortalSessionState>();
        services.AddScoped<IAppointmentPortalService, AppointmentPortalService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IServiceAdvisorAppointmentService, ServiceAdvisorAppointmentService>();
        services.AddScoped<IEmailTemplateRenderer, FileEmailTemplateRenderer>();
        services.AddScoped<ICustomerAccountService, CustomerAccountService>();
        services.AddScoped<IVehiclePhotoService, VehiclePhotoService>();
        services.AddScoped<IVehicleDetailsService, VehicleDetailsService>();
        services.AddScoped<IPortalContextService, PortalContextService>();
        services.AddScoped<IWorkOrderPortalService, WorkOrderPortalService>();
        services.AddScoped<IEstimateCatalogService, EstimateCatalogService>();
        services.AddScoped<IInvoicePaymentService, InvoicePaymentService>();

        services.AddHttpClient<IVpicVehicleDecoder, VpicVehicleDecoder>(client =>
        {
            client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TPGLLC.Web/1.0");
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(PortalPolicies.Customer, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Customer");
            })
            .AddPolicy(PortalPolicies.ServiceAdvisor, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("ServiceAdvisor");
            })
            .AddPolicy(PortalPolicies.Technician, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Technician");
            })
            .AddPolicy(PortalPolicies.Finance, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Finance");
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
