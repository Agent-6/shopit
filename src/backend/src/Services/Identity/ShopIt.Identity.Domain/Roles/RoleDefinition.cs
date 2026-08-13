using ShopIt.Identity.Domain.Permissions;

namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// Declarative definition of a built-in role, modeled after ABP's role definitions.
/// A role is identified by a <see cref="RoleName"/> value object and declares the
/// <see cref="PermissionName"/> keys granted to it by default.
/// </summary>
public record RoleDefinition(
    RoleName Name,
    string? DisplayName = null,
    string? Description = null,
    bool IsDefault = false,
    bool IsStatic = true,
    IReadOnlyList<PermissionName>? DefaultPermissions = null)
{
    /// <summary>
    /// When <c>true</c>, the role is granted every permission in the catalog
    /// (admin semantics — admin implicitly owns all permissions).
    /// </summary>
    public bool GrantsAllPermissions => DefaultPermissions is null;
}
