using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Catalog.Domain.Permissions;

/// <summary>
/// Canonical permission names owned by the Catalog service, as <see cref="PermissionName"/> value
/// objects. They are declared here — see <see cref="ShopItCatalogPermissionDefinitionProvider"/> — and
/// published to the Identity service's permission catalog at startup, so a change to them does not
/// require redeploying Identity (see <c>docs/adr/0003-permission-catalog-publishing-hook.md</c>).
/// </summary>
/// <remarks>
/// These two names were declared by the #24 placeholder (in Notifications) until this service existed.
/// Ownership moved without a data migration: Identity's synchronizer keys catalog entries by permission
/// name, so republishing under <c>SourceService = "Catalog"</c> updates the existing rows in place.
/// </remarks>
public static class ShopItCatalogPermissions
{
    /// <summary>The permission group these permissions belong to.</summary>
    public static readonly PermissionGroupName Catalog = new("Catalog");

    /// <summary>Browse products and categories in the catalog.</summary>
    public static readonly PermissionName View = new("catalog.view");

    /// <summary>Create, edit and remove products and categories in the catalog.</summary>
    public static readonly PermissionName Write = new("catalog.write");
}
