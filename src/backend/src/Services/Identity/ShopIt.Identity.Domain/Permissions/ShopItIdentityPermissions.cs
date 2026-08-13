namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Canonical permission names for the Identity service, as <see cref="PermissionName"/>
/// value objects. Permission names are stored as claim types on roles and users
/// (e.g. <c>user.create</c> = <c>true</c>), matching the ABP-style convention where
/// permissions are string identifiers that can be granted to roles or individual users.
/// </summary>
public static class ShopItIdentityPermissions
{
    public static class Groups
    {
        public static readonly PermissionGroupName UserManagement = new("UserManagement");
        public static readonly PermissionGroupName RoleManagement = new("RoleManagement");
        public static readonly PermissionGroupName TenantManagement = new("TenantManagement");
    }

    public static class Users
    {
        public static readonly PermissionName View = new("user.view");
        public static readonly PermissionName Create = new("user.create");
        public static readonly PermissionName Update = new("user.update");
        public static readonly PermissionName Delete = new("user.delete");
        public static readonly PermissionName ManageRoles = new("user.manage-roles");
        public static readonly PermissionName ManagePermissions = new("user.manage-permissions");
        public static readonly PermissionName ManageClaims = new("user.manage-claims");
        public static readonly PermissionName LockUnlock = new("user.lock-unlock");
        public static readonly PermissionName ResetPassword = new("user.reset-password");
    }

    public static class Roles
    {
        public static readonly PermissionName View = new("role.view");
        public static readonly PermissionName Create = new("role.create");
        public static readonly PermissionName Update = new("role.update");
        public static readonly PermissionName Delete = new("role.delete");
        public static readonly PermissionName ManagePermissions = new("role.manage-permissions");
    }

    public static class Tenants
    {
        public static readonly PermissionName View = new("tenant.view");
        public static readonly PermissionName Create = new("tenant.create");
        public static readonly PermissionName Update = new("tenant.update");
        public static readonly PermissionName Delete = new("tenant.delete");
        public static readonly PermissionName ActivateDeactivate = new("tenant.activate-deactivate");
    }
}
