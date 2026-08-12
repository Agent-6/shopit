namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Canonical permission names for the Identity service. Permission names are stored as
/// claim types on roles and users (e.g. <c>user.create</c> = <c>true</c>), matching the
/// ABP-style convention where permissions are string identifiers that can be granted to
/// roles or individual users.
/// </summary>
public static class ShopItIdentityPermissions
{
    public static class Groups
    {
        public const string UserManagement = "UserManagement";
        public const string RoleManagement = "RoleManagement";
        public const string TenantManagement = "TenantManagement";
    }

    public static class Users
    {
        public const string View = "user.view";
        public const string Create = "user.create";
        public const string Update = "user.update";
        public const string Delete = "user.delete";
        public const string ManageRoles = "user.manage-roles";
        public const string ManagePermissions = "user.manage-permissions";
        public const string ManageClaims = "user.manage-claims";
        public const string LockUnlock = "user.lock-unlock";
        public const string ResetPassword = "user.reset-password";
    }

    public static class Roles
    {
        public const string View = "role.view";
        public const string Create = "role.create";
        public const string Update = "role.update";
        public const string Delete = "role.delete";
        public const string ManagePermissions = "role.manage-permissions";
    }

    public static class Tenants
    {
        public const string View = "tenant.view";
        public const string Create = "tenant.create";
        public const string Update = "tenant.update";
        public const string Delete = "tenant.delete";
        public const string ActivateDeactivate = "tenant.activate-deactivate";
    }
}
