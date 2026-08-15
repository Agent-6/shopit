namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Defines a single permission in the system.
/// </summary>
/// <param name="Name">The unique permission identifier, as a value object (also used as the claim type when granted).</param>
/// <param name="DisplayName">A human-readable name shown in the UI.</param>
/// <param name="Description">An optional longer description.</param>
/// <param name="MultiTenancySide">The side(s) the permission is available on (defaults to <see cref="PermissionMultiTenancySide.Both"/>).</param>
public record PermissionDefinition(
    PermissionName Name,
    string DisplayName,
    string? Description = null,
    PermissionMultiTenancySide MultiTenancySide = PermissionMultiTenancySide.Both);
