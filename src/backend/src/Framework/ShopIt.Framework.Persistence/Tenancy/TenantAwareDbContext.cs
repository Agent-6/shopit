using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Framework.Persistence.Tenancy;

/// <summary>
/// Base <see cref="DbContext"/> for a service that owns tenant-scoped data. Call
/// <see cref="ApplyTenantFilters"/> from <c>OnModelCreating</c> to give every
/// <see cref="ITenantEntity"/> in the model a tenant query filter and <c>TenantId</c> index.
/// </summary>
/// <remarks>
/// <para>
/// This is the supported opt-in to multi-tenancy. A service that does not own tenant data — the tenant
/// registry itself, or a service like Notifications whose only tables are messaging infrastructure —
/// must <em>not</em> use it.
/// </para>
/// <para>
/// <strong>Why the filter lives here and not in a shared extension method.</strong> EF Core caches the
/// model per context type, and re-evaluates a query filter on every query <em>only if the expression is
/// rooted at the context instance</em>. Anything else — a captured local, a method parameter, a field on
/// another object — is evaluated once when <c>OnModelCreating</c> runs and baked into the cached model,
/// so the first tenant seen wins and every later query silently filters by it.
/// </para>
/// <para>
/// Below, the lambda is written inside this context class and captures only <c>this</c>, so the compiler
/// emits <c>Expression.Constant(this)</c> and the reference <em>is</em> rooted.
/// <strong>Do not refactor this into a <c>ModelBuilder</c> extension taking <see cref="ICurrentTenant"/> as
/// a parameter</strong> — the parameter would be captured through a closure display class, the reference
/// would no longer be rooted, and tenant isolation would break with no exception and no warning.
/// </para>
/// <para>
/// See <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </para>
/// </remarks>
public abstract class TenantAwareDbContext<TContext>(
    DbContextOptions<TContext> options,
    ICurrentTenant currentTenant)
    : DbContext(options)
    where TContext : DbContext
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// Applies the tenant query filter and <c>TenantId</c> index to every entity in the model that
    /// implements <see cref="ITenantEntity"/>. Call from <c>OnModelCreating</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Call this at the end of <c>OnModelCreating</c></strong>, after the context has registered
    /// every entity. It filters the entity types present in the model at the moment it runs, so anything
    /// registered later is missed.
    /// </para>
    /// <para>
    /// It is deliberately not applied automatically from this class's own <c>OnModelCreating</c>. That was
    /// implemented and measured, and it leaks: a base call runs before the derived context finishes
    /// building its model, so an entity registered afterwards — for example one that has an
    /// <c>IEntityTypeConfiguration</c> but no <c>DbSet</c> — is left permanently unfiltered.
    /// </para>
    /// <para>
    /// Host-level entities are excluded by simply not implementing <see cref="ITenantEntity"/> — their
    /// absence of a filter is deliberate, not an oversight.
    /// </para>
    /// <para>
    /// Neither shape is loud when it is got wrong. See <c>docs/adr/0001-tenant-isolation-model.md</c>.
    /// </para>
    /// </remarks>
    protected void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        var applyTenantFilterMethod = typeof(TenantAwareDbContext<TContext>)
            .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                applyTenantFilterMethod?.MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [modelBuilder]);
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasIndex(e => e.TenantId);

        // Rooted at the context instance by construction: this lambda captures only `this`.
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => _currentTenant.Id == Guid.Empty || e.TenantId == _currentTenant.Id);
    }
}
