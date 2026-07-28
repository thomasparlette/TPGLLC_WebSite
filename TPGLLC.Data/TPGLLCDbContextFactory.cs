using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TPGLLC.Data;

public sealed class TPGLLCDbContextFactory : IDesignTimeDbContextFactory<TPGLLCDbContext>
{
    public TPGLLCDbContext CreateDbContext(string[] args)
    {
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(basePath, $"appsettings.{environment}.json"), optional: true)
            .AddEnvironmentVariables()
            .Build(); ;

        var connectionString =
            configuration.GetConnectionString("WebsiteDb")
            ?? configuration["ConnectionStrings__WebsiteDb"]
            ?? throw new InvalidOperationException(
                "Connection string 'WebsiteDb' was not found in configuration or the ConnectionStrings__WebsiteDb environment variable.");

        var optionsBuilder = new DbContextOptionsBuilder<TPGLLCDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TPGLLCDbContext(optionsBuilder.Options);
    }
}