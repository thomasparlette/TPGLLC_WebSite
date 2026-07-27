using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TPGLLC.Data;

public sealed class TPGLLCDbContextFactory : IDesignTimeDbContextFactory<TPGLLCDbContext>
{
    public TPGLLCDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__WebsiteDb")
            ?? throw new InvalidOperationException(
                "Connection string 'WebsiteDb' was not found in the ConnectionStrings__WebsiteDb environment variable.");

        var optionsBuilder = new DbContextOptionsBuilder<TPGLLCDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TPGLLCDbContext(optionsBuilder.Options);
    }
}