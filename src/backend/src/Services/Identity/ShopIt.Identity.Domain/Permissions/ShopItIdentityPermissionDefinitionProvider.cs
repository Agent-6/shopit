namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Defines the standard permissions for the Identity service, modeled after ABP's
/// <c>IdentityPermissionDefinitionProvider</c>. The same provider is used both to expose
/// the permission catalog via the API and to seed permission claims into the default roles.
/// Groups and permissions are keyed by value-object records and registered through the
/// fluent <c>AddGroup</c> / <c>AddPermission</c> extension methods.
/// </summary>
public class ShopItIdentityPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    private readonly IReadOnlyList<PermissionGroupDefinition> _groups;

    public ShopItIdentityPermissionDefinitionProvider()
    {
        var context = new PermissionDefinitionContext();
        Define(context);
        _groups = context.GetGroups();
    }

    public void Define(IPermissionDefinitionContext context)
    {
        ShopItIdentityPermissions.Groups.UserManagement.AddGroup(context, "User Management")
            .AddPermission(ShopItIdentityPermissions.Users.View, "View users", "View user accounts and their details.")
            .AddPermission(ShopItIdentityPermissions.Users.Create, "Create users", "Create new user accounts.")
            .AddPermission(ShopItIdentityPermissions.Users.Update, "Update users", "Edit user profile information.")
            .AddPermission(ShopItIdentityPermissions.Users.Delete, "Delete users", "Soft or permanently delete user accounts.")
            .AddPermission(ShopItIdentityPermissions.Users.ManageRoles, "Manage user roles", "Assign and remove roles for users.")
            .AddPermission(ShopItIdentityPermissions.Users.ManagePermissions, "Manage user permissions", "Grant or deny direct permissions for users.")
            .AddPermission(ShopItIdentityPermissions.Users.ManageClaims, "Manage user claims", "Add and remove claims on user accounts.")
            .AddPermission(ShopItIdentityPermissions.Users.LockUnlock, "Lock/unlock users", "Lock and unlock user accounts.")
            .AddPermission(ShopItIdentityPermissions.Users.ResetPassword, "Reset user passwords", "Set a new password for a user.");

        ShopItIdentityPermissions.Groups.RoleManagement.AddGroup(context, "Role Management")
            .AddPermission(ShopItIdentityPermissions.Roles.View, "View roles", "View roles and their permissions.")
            .AddPermission(ShopItIdentityPermissions.Roles.Create, "Create roles", "Create new roles.")
            .AddPermission(ShopItIdentityPermissions.Roles.Update, "Update roles", "Edit roles and their descriptions.")
            .AddPermission(ShopItIdentityPermissions.Roles.Delete, "Delete roles", "Delete roles that are not assigned to users.")
            .AddPermission(ShopItIdentityPermissions.Roles.ManagePermissions, "Manage role permissions", "Grant or remove permissions on roles.");

        ShopItIdentityPermissions.Groups.TenantManagement.AddGroup(context, "Tenant Management")
            .AddPermission(ShopItIdentityPermissions.Tenants.View, "View tenants", "View tenants in the system.")
            .AddPermission(ShopItIdentityPermissions.Tenants.Create, "Create tenants", "Create new tenants.")
            .AddPermission(ShopItIdentityPermissions.Tenants.Update, "Update tenants", "Edit tenant information.")
            .AddPermission(ShopItIdentityPermissions.Tenants.Delete, "Delete tenants", "Delete tenants.")
            .AddPermission(ShopItIdentityPermissions.Tenants.ActivateDeactivate, "Activate/deactivate tenants", "Change the active state of tenants.");
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;

    public IEnumerable<PermissionDefinition> GetAll() =>
        _groups.SelectMany(g => g.Permissions);
}
