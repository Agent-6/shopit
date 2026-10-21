using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Persistence.Data;

/// <summary>
/// Creates a <see cref="CatalogDbContext"/> for the EF Core CLI so migrations can be generated without
/// starting the service.
/// </summary>
/// <remarks>
/// The tenant is <c>null</c>: design time only compiles the model, it never evaluates a query, so the
/// tenant filter never dereferences it. This mirrors Identity's design-time factory — the CLI has no
/// request scope to resolve a real <see cref="ICurrentTenant"/> from.
/// </remarks>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();

        // Dummy connection string: only used by the CLI to decide the provider.
        optionsBuilder.UseNpgsql("Host=localhost;Database=catalog-db;Username=postgres;Password=postgres");

        return new CatalogDbContext(optionsBuilder.Options, currentTenant: null!);
    }
}
