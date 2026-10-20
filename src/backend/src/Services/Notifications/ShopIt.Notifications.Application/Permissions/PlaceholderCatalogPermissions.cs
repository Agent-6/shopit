using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Notifications.Application.Permissions;

/// <summary>
/// **PLACEHOLDER (#24).** Canonical names for the permission pair the product catalog will own in
/// M1. They live here only long enough to prove that a service can declare permissions, publish
/// them through <c>AddPermissionCatalogPublishing</c>, have them land in Identity's catalog and be
/// enforced by <c>RequirePermission</c>.
/// </summary>
/// <remarks>
/// Delete this class with <see cref="PlaceholderCatalogPermissionDefinitionProvider"/> when the real
/// catalog service declares these names; nothing else depends on it.
/// </remarks>
public static class PlaceholderCatalogPermissions
{
    /// <summary>The permission group these placeholder permissions belong to.</summary>
    public static readonly PermissionGroupName Catalog = new("Catalog");

    /// <summary>Browse products in the catalog.</summary>
    public static readonly PermissionName View = new("catalog.view");

    /// <summary>Create, edit and remove products in the catalog.</summary>
    public static readonly PermissionName Write = new("catalog.write");
}
