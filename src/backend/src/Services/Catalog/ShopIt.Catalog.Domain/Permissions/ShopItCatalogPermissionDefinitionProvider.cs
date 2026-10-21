using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Catalog.Domain.Permissions;

/// <summary>
/// Defines the Catalog service's permissions. Every service owns the permissions it enforces; the
/// Catalog API publishes this catalog to the Identity service at startup, which persists it and grants
/// any new permissions to the Admin role without Identity being redeployed.
/// </summary>
public class ShopItCatalogPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <summary>Source-service name stamped on this catalog's entries in Identity.</summary>
    public const string SourceService = "Catalog";

    private readonly IReadOnlyList<PermissionGroupDefinition> _groups;

    public ShopItCatalogPermissionDefinitionProvider()
    {
        var context = new PermissionDefinitionContext();
        Define(context);
        _groups = context.GetGroups();
    }

    /// <inheritdoc />
    public void Define(IPermissionDefinitionContext context)
    {
        // Both sides: the catalog is served to host-scoped and tenant-scoped callers alike, so neither
        // permission is restricted to one side. Narrowing that later is a change to this method, not to
        // any consumer.
        ShopItCatalogPermissions.Catalog
            .AddGroup(context, "Catalog")
            .AddPermission(
                ShopItCatalogPermissions.View,
                "View catalog",
                "Browse products in the catalog.")
            .AddPermission(
                ShopItCatalogPermissions.Write,
                "Manage catalog",
                "Create, edit and remove products in the catalog.");
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;

    /// <inheritdoc />
    public IEnumerable<PermissionDefinition> GetAll() =>
        _groups.SelectMany(group => group.Permissions);
}
