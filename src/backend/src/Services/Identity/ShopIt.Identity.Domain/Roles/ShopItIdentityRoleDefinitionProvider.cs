using ShopIt.Identity.Domain.Permissions;

namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// Defines the built-in roles and the permission keys each one is granted by default.
/// Mirrors ABP's role definitions and replaces the hardcoded permission dictionary that
/// used to live in the API's startup seeding.
/// </summary>
public class ShopItIdentityRoleDefinitionProvider : IRoleDefinitionProvider
{
    public IReadOnlyList<RoleDefinition> GetAll() =>
    [
        new RoleDefinition(
            ShopItIdentityRoles.Admin,
            "Administrator",
            "Full access to every permission in the system.",
            IsDefault: false,
            DefaultPermissions: null), // null = all permissions from the catalog

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
                ShopItIdentityPermissions.Tenants.View,
                ShopItIdentityPermissions.Tenants.Create,
                ShopItIdentityPermissions.Tenants.Update,
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
                ShopItIdentityPermissions.Tenants.View,
            ]),
    ];
}
