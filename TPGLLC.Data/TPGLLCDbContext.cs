using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data.Configurations;
using TPGLLC.Shared.Identity;
using TPGLLC.Data.Entities;

namespace TPGLLC.Data;

public sealed class TPGLLCDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public TPGLLCDbContext(DbContextOptions<TPGLLCDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppointmentRequest> AppointmentRequests => Set<AppointmentRequest>();
    public DbSet<VehicleCatalogEntry> VehicleCatalogEntries => Set<VehicleCatalogEntry>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerVehicle> CustomerVehicles => Set<CustomerVehicle>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TPGLLCDbContext).Assembly);

        modelBuilder.Entity<AppointmentRequest>(entity =>
        {
            entity.ToTable("AppointmentRequests");
            entity.HasKey(x => x.RequestId);

            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.VehicleType).HasMaxLength(30);
            entity.Property(x => x.VehicleYear).HasMaxLength(20);
            entity.Property(x => x.VehicleMake).HasMaxLength(120);
            entity.Property(x => x.VehicleModel).HasMaxLength(120);
            entity.Property(x => x.Vin).HasMaxLength(17);
            entity.Property(x => x.Mileage).HasMaxLength(50);
            entity.Property(x => x.PreferredDate).HasMaxLength(20);
            entity.Property(x => x.PreferredTime).HasMaxLength(20);
            entity.Property(x => x.ServiceNeeded).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Company).HasMaxLength(200);

            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.SubmittedAtUtc);
        });

        modelBuilder.Entity<VehicleCatalogEntry>(entity =>
        {
            entity.ToTable("VehicleCatalogEntries");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.VehicleType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Make).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(120).IsRequired();

            entity.Property(x => x.MakeId).IsRequired();
            entity.Property(x => x.ModelId).IsRequired();

            entity.HasIndex(x => new { x.VehicleType, x.ModelYear, x.MakeId, x.ModelId })
                  .IsUnique();

            entity.HasIndex(x => new { x.VehicleType, x.ModelYear, x.Make, x.Model });
        });
    }
}