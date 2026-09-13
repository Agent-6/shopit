namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Supplies a catalog of permissions known to a service, organized into groups.
/// Each microservice implements this provider for its own permissions. Providers
/// declare their catalog through <see cref="Define"/> (ABP-style), and the result is
/// exposed via <see cref="GetGroups"/> / <see cref="GetAll"/>. The Identity service
/// collects these catalogs (via integration events) into its persisted permission
/// catalog, which is what authorization and the permission management UIs read from.
/// </summary>
public interface IPermissionDefinitionProvider
{
    /// <summary>
    /// Registers permission groups and permissions, keyed by value-object records.
    /// Called once at construction time.
    /// </summary>
    void Define(IPermissionDefinitionContext context);

    /// <summary>
    /// Returns all permission groups with their permissions.
    /// </summary>
    IReadOnlyList<PermissionGroupDefinition> GetGroups();

    /// <summary>
    /// Returns every permission across all groups.
    /// </summary>
    IEnumerable<PermissionDefinition> GetAll();
}
