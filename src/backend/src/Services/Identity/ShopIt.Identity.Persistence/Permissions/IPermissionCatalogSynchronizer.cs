using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Persistence.Permissions;

/// <summary>
/// Merges a service's permission catalog into the persisted permission catalog: upserts
/// definitions, grants newly added permissions to the Admin role in every tenant, and
/// removes catalog entries (with their role-claim grants) that the service no longer
/// publishes. Used both by the integration event handler (catalogs published by other
/// services) and by Identity's own startup seeding (its own catalog).
/// </summary>
public interface IPermissionCatalogSynchronizer
{
    /// <summary>
    /// Synchronizes the catalog to match <paramref name="groups"/> for
    /// <paramref name="sourceService"/>: definitions the service still publishes are
    /// upserted, definitions it previously published but no longer does are deleted and
    /// revoked from every role, and permission names new to the catalog are granted to
    /// every Admin role. Idempotent — safe to call repeatedly (e.g. on every startup or on
    /// republished catalogs).
    /// </summary>
    Task SynchronizeAsync(
        string sourceService,
        IReadOnlyList<PermissionGroupDefinition> groups,
        CancellationToken cancellationToken = default);
}
