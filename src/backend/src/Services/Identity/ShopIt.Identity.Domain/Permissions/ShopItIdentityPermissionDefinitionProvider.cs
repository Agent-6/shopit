namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Defines the standard permissions for the Identity service, modeled after ABP's
/// <c>IdentityPermissionDefinitionProvider</c>. The same provider is used both to expose
/// the permission catalog via the API and to seed permission claims into the default roles.
/// </summary>
public class ShopItIdentityPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    private readonly IReadOnlyList<PermissionGroupDefinition> _groups = BuildGroups();

    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;

    public IEnumerable<PermissionDefinition> GetAll() =>
        _groups.SelectMany(g => g.Permissions);

    private static IReadOnlyList<PermissionGroupDefinition> BuildGroups() =>
    [
        new PermissionGroupDefinition(
            ShopItIdentityPermissions.Groups.UserManagement,
            "User Management",
            [
                new PermissionDefinition(ShopItIdentityPermissions.Users.View, "View users", "View user accounts and their details."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.Create, "Create users", "Create new user accounts."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.Update, "Update users", "Edit user profile information."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.Delete, "Delete users", "Soft or permanently delete user accounts."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.ManageRoles, "Manage user roles", "Assign and remove roles for users."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.ManagePermissions, "Manage user permissions", "Grant or deny direct permissions for users."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.ManageClaims, "Manage user claims", "Add and remove claims on user accounts."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.LockUnlock, "Lock/unlock users", "Lock and unlock user accounts."),
                new PermissionDefinition(ShopItIdentityPermissions.Users.ResetPassword, "Reset user passwords", "Set a new password for a user."),
            ]),
        new PermissionGroupDefinition(
            ShopItIdentityPermissions.Groups.RoleManagement,
            "Role Management",
            [
                new PermissionDefinition(ShopItIdentityPermissions.Roles.View, "View roles", "View roles and their permissions."),
                new PermissionDefinition(ShopItIdentityPermissions.Roles.Create, "Create roles", "Create new roles."),
                new PermissionDefinition(ShopItIdentityPermissions.Roles.Update, "Update roles", "Edit roles and their descriptions."),
                new PermissionDefinition(ShopItIdentityPermissions.Roles.Delete, "Delete roles", "Delete roles that are not assigned to users."),
                new PermissionDefinition(ShopItIdentityPermissions.Roles.ManagePermissions, "Manage role permissions", "Grant or remove permissions on roles."),
            ]),
        new PermissionGroupDefinition(
            ShopItIdentityPermissions.Groups.TenantManagement,
            "Tenant Management",
            [
                new PermissionDefinition(ShopItIdentityPermissions.Tenants.View, "View tenants", "View tenants in the system."),
                new PermissionDefinition(ShopItIdentityPermissions.Tenants.Create, "Create tenants", "Create new tenants."),
                new PermissionDefinition(ShopItIdentityPermissions.Tenants.Update, "Update tenants", "Edit tenant information."),
                new PermissionDefinition(ShopItIdentityPermissions.Tenants.Delete, "Delete tenants", "Delete tenants."),
                new PermissionDefinition(ShopItIdentityPermissions.Tenants.ActivateDeactivate, "Activate/deactivate tenants", "Change the active state of tenants."),
            ]),
    ];
}
