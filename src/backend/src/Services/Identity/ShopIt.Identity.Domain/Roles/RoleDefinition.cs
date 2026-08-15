using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// Declarative definition of a built-in role, modeled after ABP's role definitions.
/// A role is identified by a <see cref="RoleName"/> value object and declares the
/// <see cref="PermissionName"/> keys granted to it by default and the multi-tenancy
/// side(s) it is available on. Roles declared host-only or tenant-only are only
/// provisioned on that side; <see cref="PermissionMultiTenancySide.Both"/> (the
/// default) is provisioned everywhere.
/// </summary>
public record RoleDefinition(
    RoleName Name,
    string? DisplayName = null,
    string? Description = null,
    bool IsDefault = false,
    bool IsStatic = true,
    IReadOnlyList<PermissionName>? DefaultPermissions = null,
    PermissionMultiTenancySide Side = PermissionMultiTenancySide.Both)
{
    /// <summary>
    /// When <c>true</c>, the role is granted every permission as it enters the permission
    /// catalog: seeding grants it everything currently in the catalog and, when the
    /// Identity service receives a newly published catalog, any permissions it does not
    /// already hold are granted to it (admin semantics).
    /// </summary>
    public bool GrantsAllPermissions => DefaultPermissions is null;
}
