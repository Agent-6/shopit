using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Tenancy.Domain.Permissions;

/// <summary>
/// Defines the Tenancy service's permissions (tenant management). Every microservice owns
/// its own permission definitions; the Tenancy API publishes this catalog to the Identity
/// service at startup, which persists it and grants any new permissions to the Admin role.
/// </summary>
public class ShopItTenancyPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <summary>Source-service name stamped on this catalog's entries in Identity.</summary>
    public const string SourceService = "Tenancy";

    private readonly IReadOnlyList<PermissionGroupDefinition> _groups;

    public ShopItTenancyPermissionDefinitionProvider()
    {
        var context = new PermissionDefinitionContext();
        Define(context);
        _groups = context.GetGroups();
    }

    public void Define(IPermissionDefinitionContext context)
    {
        // Tenant lifecycle operations are host-side concerns: creating, deleting or
        // deactivating tenants is done by the host, while viewing and updating a tenant
        // (its own record) is available on both sides.
        ShopItTenancyPermissions.TenantManagement.AddGroup(context, "Tenant Management")
            .AddPermission(ShopItTenancyPermissions.View, "View tenants", "View tenants in the system.")
            .AddPermission(ShopItTenancyPermissions.Create, "Create tenants", "Create new tenants.",
                multiTenancySide: PermissionMultiTenancySide.Host)
            .AddPermission(ShopItTenancyPermissions.Update, "Update tenants", "Edit tenant information.")
            .AddPermission(ShopItTenancyPermissions.Delete, "Delete tenants", "Delete tenants.",
                multiTenancySide: PermissionMultiTenancySide.Host)
            .AddPermission(ShopItTenancyPermissions.ActivateDeactivate, "Activate/deactivate tenants", "Change the active state of tenants.",
                multiTenancySide: PermissionMultiTenancySide.Host);
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;

    public IEnumerable<PermissionDefinition> GetAll() =>
        _groups.SelectMany(g => g.Permissions);
}
