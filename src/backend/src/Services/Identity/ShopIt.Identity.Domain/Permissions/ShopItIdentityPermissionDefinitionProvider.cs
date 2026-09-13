using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Domain.Permissions;

/// <summary>
/// Defines the Identity service's own permissions (user and role management), modeled
/// after ABP's <c>IdentityPermissionDefinitionProvider</c>. This provider describes only
/// <em>this</em> service's permissions: other microservices define their own providers and
/// publish them to the Identity service, which persists them into its permission catalog.
/// The catalog (not this provider) is what the API and seeding consume.
/// </summary>
public class ShopItIdentityPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <summary>Source-service name stamped on Identity's catalog entries.</summary>
    public const string SourceService = "Identity";

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
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;

    public IEnumerable<PermissionDefinition> GetAll() =>
        _groups.SelectMany(g => g.Permissions);
}
