using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Persistence;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Domain.Users;
using ShopIt.Framework.Persistence.Auditing;

namespace ShopIt.Identity.Persistence.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentTenant currentTenant,
    ICurrentUser? currentUser = null) : IdentityDbContext<
    User,
    Role,
    Guid,
    UserClaim,
    UserRole,
    UserLogin,
    RoleClaim,
    UserToken
>(options)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ICurrentUser? _currentUser = currentUser;

    /// <summary>
    /// Persisted permission catalog: the union of every service's permission definitions.
    /// System-wide (not tenant-scoped) — see <see cref="Entities.PermissionCatalogEntry"/>.
    /// </summary>
    public DbSet<PermissionCatalogEntry> PermissionCatalogEntries => Set<PermissionCatalogEntry>();

    /// <inheritdoc />
    /// <remarks>
    /// Identity saves through <c>UserManager</c> and <c>RoleManager</c> as much as through the unit of
    /// work, so audit stamping happens here too — every write through this context is stamped, whichever
    /// path triggered it (ADR-0004).
    /// </remarks>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AuditInfo.Apply(this, _currentUser?.Id);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AuditInfo.Apply(this, _currentUser?.Id);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Ignore domain events so EF Core doesn't try to map DomainEvent as an entity
        builder.Ignore<DomainEvent>();

        ApplyTenantConfiguration(builder);
        builder.ApplyInboxOutboxConfigurations();
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    private void ApplyTenantConfiguration(ModelBuilder builder)
    {
        var applyTenantFilterMethod = typeof(ApplicationDbContext)
            .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                applyTenantFilterMethod?.MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [builder]);
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>()
            .HasIndex(e => e.TenantId);

        builder.Entity<TEntity>()
            .HasQueryFilter(e => _currentTenant.Id == Guid.Empty || e.TenantId == _currentTenant.Id);
    }
}
