using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Framework.Persistence.Tenancy;

/// <summary>
/// Fails the model build when a tenant-scoped entity has no query filter.
/// </summary>
/// <remarks>
/// <para>
/// Tenant isolation is enforced by a query filter, and a filter that is missing does not error — it
/// silently returns other tenants' rows. Every mistake that produces that state (forgetting to call
/// <see cref="TenantAwareDbContext{TContext}.ApplyTenantFilters"/>, or registering an entity after it
/// has run) is invisible at runtime except as a data leak.
/// </para>
/// <para>
/// This convention inspects the <em>final</em> model, so it sees entity types registered anywhere in
/// <c>OnModelCreating</c>, and converts the leak into a startup failure.
/// </para>
/// <para>
/// A host-level entity does not implement <see cref="ITenantEntity"/> and is therefore not reported —
/// its absence of a filter is correct. See <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </para>
/// </remarks>
internal sealed class TenantFilterGuardConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        var unfiltered = modelBuilder.Metadata
            .GetEntityTypes()
            .Where(entityType => typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            .Where(entityType => !entityType.GetDeclaredQueryFilters().Any())
            .Select(entityType => entityType.ClrType.FullName ?? entityType.ClrType.Name)
            .ToList();

        if (unfiltered.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Tenant-scoped entities have no query filter: {string.Join(", ", unfiltered)}. " +
            "Their reads would return other tenants' rows. This means either ApplyTenantFilters was not " +
            "called, or the entity was registered after it ran. Call ApplyTenantFilters at the END of " +
            "OnModelCreating, after every entity is registered. " +
            "See docs/adr/0001-tenant-isolation-model.md.");
    }
}
