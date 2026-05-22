using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopIt.Tenancy.Persistence.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    public TenancyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenancyDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=tenancy-db;Username=postgres;Password=postgres");

        return new TenancyDbContext(optionsBuilder.Options);
    }
}
