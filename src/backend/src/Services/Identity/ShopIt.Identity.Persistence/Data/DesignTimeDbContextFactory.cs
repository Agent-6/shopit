using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Persistence.Data;

public class DesignTimeDbContextFactory() : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private readonly ICurrentTenant _currentTenant = null!;
    
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        // Use a dummy string; only used by the CLI to generate migration files
        optionsBuilder.UseNpgsql("Host=localhost;Database=identity-db;Username=postgres;Password=postgres");

        return new ApplicationDbContext(optionsBuilder.Options, _currentTenant);
    }
}
