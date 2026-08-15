using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Tenancy.Domain.Permissions;

/// <summary>
/// Canonical permission names for the Tenancy service, as <see cref="PermissionName"/>
/// value objects. These are defined <em>by</em> the Tenancy service (see
/// <see cref="ShopItTenancyPermissionDefinitionProvider"/>) and published to the Identity
/// service's permission catalog via an integration event, so the Identity project does not
/// need to be redeployed when Tenancy's permissions change.
/// </summary>
public static class ShopItTenancyPermissions
{
    public static readonly PermissionGroupName TenantManagement = new("TenantManagement");

    public static readonly PermissionName View = new("tenant.view");
    public static readonly PermissionName Create = new("tenant.create");
    public static readonly PermissionName Update = new("tenant.update");
    public static readonly PermissionName Delete = new("tenant.delete");
    public static readonly PermissionName ActivateDeactivate = new("tenant.activate-deactivate");
}
