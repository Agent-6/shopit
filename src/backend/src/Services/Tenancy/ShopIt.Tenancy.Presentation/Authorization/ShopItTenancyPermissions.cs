namespace ShopIt.Tenancy.Presentation.Authorization;

/// <summary>
/// Canonical permission names for the Tenancy service. These mirror the <c>Tenants</c> group
/// defined in the Identity service's permission catalog and are granted as claims on roles
/// and users there.
/// </summary>
public static class ShopItTenancyPermissions
{
    public const string View = "tenant.view";
    public const string Create = "tenant.create";
    public const string Update = "tenant.update";
    public const string Delete = "tenant.delete";
    public const string ActivateDeactivate = "tenant.activate-deactivate";
}
