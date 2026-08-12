namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Supplies the catalog of permissions known to the system, organized into groups.
/// </summary>
public interface IPermissionDefinitionProvider
{
    /// <summary>
    /// Returns all permission groups with their permissions.
    /// </summary>
    IReadOnlyList<PermissionGroupDefinition> GetGroups();

    /// <summary>
    /// Returns every permission across all groups.
    /// </summary>
    IEnumerable<PermissionDefinition> GetAll();
}
