using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopIt.Identity.Persistence.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        // Use a dummy string; only used by the CLI to generate migration files
        optionsBuilder.UseNpgsql("Host=localhost;Database=identity-db;Username=postgres;Password=postgres");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
