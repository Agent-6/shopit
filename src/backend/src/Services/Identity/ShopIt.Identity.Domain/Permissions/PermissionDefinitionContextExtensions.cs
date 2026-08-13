namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Fluent registration API for permission definitions. <c>AddGroup</c> and
/// <c>AddPermission</c> are extension methods on the value-object records
/// (<see cref="PermissionGroupName"/>, <see cref="PermissionGroupDefinition"/>),
/// enabling chainable, typed definition blocks. Both return the group so the chain
/// can continue:
/// <code>
/// Groups.UserManagement.AddGroup(context, "User Management")
///     .AddPermission(Permissions.Users.View, "View users", "View user accounts.")
///     .AddPermission(Permissions.Users.Create, "Create users");
/// </code>
/// </summary>
public static class PermissionDefinitionContextExtensions
{
    /// <summary>
    /// Registers a permission group, keyed by the <see cref="PermissionGroupName"/> value object.
    /// </summary>
    public static PermissionGroupDefinition AddGroup(
        this PermissionGroupName groupName,
        IPermissionDefinitionContext context,
        string displayName)
        => context.AddGroup(groupName, displayName);

    /// <summary>
    /// Adds a permission to the group, keyed by the <see cref="PermissionName"/> value object.
    /// Returns the group so further permissions can be chained.
    /// </summary>
    public static PermissionGroupDefinition AddPermission(
        this PermissionGroupDefinition group,
        PermissionName permissionName,
        string displayName,
        string? description = null)
    {
        group.Append(new PermissionDefinition(permissionName, displayName, description));
        return group;
    }

    /// <summary>
    /// Alternative receiver: adds a permission, invoking it from the <see cref="PermissionName"/>
    /// value object itself. Returns the group so further permissions can be chained.
    /// </summary>
    public static PermissionGroupDefinition AddPermission(
        this PermissionName permissionName,
        PermissionGroupDefinition group,
        string displayName,
        string? description = null)
        => group.AddPermission(permissionName, displayName, description);
}
