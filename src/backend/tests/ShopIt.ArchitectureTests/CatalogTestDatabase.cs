using Microsoft.EntityFrameworkCore;
using ShopIt.Catalog.Persistence.Data;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// One in-memory catalog database, and a factory for <see cref="CatalogDbContext"/> instances over it.
/// </summary>
/// <remarks>
/// <para>
/// Each instance owns a private store (the database name is unique per instance), so tests cannot see each
/// other's rows and do not have to clean up. <see cref="CreateContext"/> hands every caller a
/// <em>separate</em> context instance over that one store, which is the shape ADR-0001's proof needs: two
/// tenants through two context instances, because a single-context test passes even when the filter is
/// baked into the cached model.
/// </para>
/// <para>
/// <strong>InMemory, and what that does and does not prove.</strong> The provider is used because it
/// requires no database and no stack to be running. It is not a stand-in for PostgreSQL for SQL
/// translation (that is where the relational provider differs), but it <em>is</em> sufficient here: the
/// tenant filter is an EF Core model artefact, evaluated per query by the query pipeline exactly like on
/// Npgsql, and the model conventions in <c>ShopIt.Framework.Persistence</c> —
/// <c>TenantFilterGuardConvention</c> included — run for every provider. The composite <c>TenantId</c>
/// indexes are the one thing InMemory ignores, and they are not part of this proof.
/// </para>
/// </remarks>
internal sealed class CatalogTestDatabase
{
    private readonly DbContextOptions<CatalogDbContext> _options;

    /// <summary>Creates an empty store, distinct from every other instance created by this class.</summary>
    public CatalogTestDatabase()
    {
        _options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"catalog-tenant-isolation-{Guid.NewGuid()}")
            .Options;
    }

    /// <summary>
    /// Creates a context over this store whose tenant scope is <paramref name="currentTenant"/>.
    /// </summary>
    /// <param name="currentTenant">The tenant scope this context queries under.</param>
    public CatalogDbContext CreateContext(FakeCurrentTenant currentTenant) => new(_options, currentTenant);
}
