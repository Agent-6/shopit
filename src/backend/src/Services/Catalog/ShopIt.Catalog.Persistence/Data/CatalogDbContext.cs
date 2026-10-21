using Microsoft.EntityFrameworkCore;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Tenancy;

namespace ShopIt.Catalog.Persistence.Data;

/// <summary>
/// The Catalog service's database context: the framework's inbox/outbox tables plus the catalog's own
/// aggregate tables.
/// </summary>
/// <remarks>
/// <para>
/// It derives from <see cref="TenantAwareDbContext{TContext}"/> because everything this service owns is
/// tenant-scoped data (a tenant's products are not another tenant's products). The base class's
/// <c>ApplyTenantFilters</c> call is what gives <see cref="Category"/> its query filter and index — see
/// <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </para>
/// <para>
/// Notifications, whose tables are only messaging infrastructure, deliberately does not do this; the
/// difference is whether the service owns tenant data, not how many tables it has.
/// </para>
/// </remarks>
public class CatalogDbContext(
    DbContextOptions<CatalogDbContext> options,
    ICurrentTenant currentTenant)
    : TenantAwareDbContext<CatalogDbContext>(options, currentTenant)
{
    /// <summary>Tenant-owned product categories.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Tenant-owned products.</summary>
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Domain events are an in-memory artefact of the aggregate, not rows: without this EF Core
        // discovers DomainEvent (through AggregateRoot's DomainEvents property) and demands a key for
        // it. Tenancy does the same, for the same reason.
        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.ApplyInboxOutboxConfigurations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // Must run last: it filters the entity types already in the model, so anything registered after
        // it would be left unfiltered.
        ApplyTenantFilters(modelBuilder);
    }
}
