using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Permissions;

namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// Defines the built-in roles and the permission keys each one is granted by default.
/// Mirrors ABP's role definitions and replaces the hardcoded permission dictionary that
/// used to live in the API's startup seeding.
/// </summary>
public class ShopItIdentityRoleDefinitionProvider : IRoleDefinitionProvider
{
    /// <summary>
    /// Permissions owned by the Tenancy service (defined in its own permission provider).
    /// Referenced by name here since the Identity domain cannot depend on the Tenancy
    /// service; the names must stay in sync with <c>ShopItTenancyPermissions</c>.
    /// </summary>
    private static readonly PermissionName TenantView = new("tenant.view");
    private static readonly PermissionName TenantCreate = new("tenant.create");
    private static readonly PermissionName TenantUpdate = new("tenant.update");

    public IReadOnlyList<RoleDefinition> GetAll() =>
    [
        new RoleDefinition(
            ShopItIdentityRoles.Admin,
            "Administrator",
            "Full access to every permission in the system.",
            IsDefault: false,
            DefaultPermissions: null), // null = granted every permission as it is seeded into the catalog

        new RoleDefinition(
            ShopItIdentityRoles.Manager,
            "Manager",
            "Read plus operational permissions.",
            DefaultPermissions:
            [
                ShopItIdentityPermissions.Users.View,
                ShopItIdentityPermissions.Users.Create,
                ShopItIdentityPermissions.Users.Update,
                ShopItIdentityPermissions.Users.ManageRoles,
                ShopItIdentityPermissions.Roles.View,
                ShopItIdentityPermissions.Roles.Create,
                ShopItIdentityPermissions.Roles.Update,
                TenantView,
                TenantCreate,
                TenantUpdate,
            ]),

        new RoleDefinition(
            ShopItIdentityRoles.User,
            "User",
            "Read-only access.",
            IsDefault: true,
            DefaultPermissions:
            [
                ShopItIdentityPermissions.Users.View,
                ShopItIdentityPermissions.Roles.View,
                TenantView,
            ]),
    ];
}
