using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Tenancy;

namespace ShopIt.Catalog.Persistence.Data;

/// <summary>
/// The Catalog service's database context. Today its schema is exactly the framework's inbox/outbox
/// tables: no catalog tables exist until products and categories land (#77/#78).
/// </summary>
/// <remarks>
/// <para>
/// It derives from <see cref="TenantAwareDbContext{TContext}"/> because everything this service will own
/// is tenant-scoped data (a tenant's products are not another tenant's products). Inheriting now means
/// the first entity that implements <see cref="ITenantEntity"/> is filtered by default instead of
/// depending on whoever writes it to remember — and the base class's convention fails the model build if
/// it is not. See <c>docs/adr/0001-tenant-isolation-model.md</c>.
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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyInboxOutboxConfigurations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // Must run last: it filters the entity types already in the model, so anything registered after
        // it would be left unfiltered. A no-op while the model holds no ITenantEntity.
        ApplyTenantFilters(modelBuilder);
    }
}
