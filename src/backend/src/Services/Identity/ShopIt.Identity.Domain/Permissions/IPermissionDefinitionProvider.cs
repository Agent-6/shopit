namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Supplies the catalog of permissions known to the system, organized into groups.
/// Providers declare their catalog through <see cref="Define"/> (ABP-style), and the
/// result is exposed via <see cref="GetGroups"/> / <see cref="GetAll"/>.
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
