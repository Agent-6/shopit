namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Accumulates permission groups and permissions while a
/// <see cref="IPermissionDefinitionProvider"/> runs its <c>Define</c> step.
/// </summary>
public interface IPermissionDefinitionContext
{
    /// <summary>
    /// Adds (or returns an existing) group keyed by <paramref name="name"/>.
    /// </summary>
    PermissionGroupDefinition AddGroup(PermissionGroupName name, string displayName);

    /// <summary>
    /// Adds a permission to the group keyed by <paramref name="groupName"/>.
    /// </summary>
    PermissionDefinition AddPermission(
        PermissionGroupName groupName,
        PermissionName name,
        string displayName,
        string? description = null);

    /// <summary>
    /// Returns all groups registered so far, in registration order.
    /// </summary>
    IReadOnlyList<PermissionGroupDefinition> GetGroups();
}
