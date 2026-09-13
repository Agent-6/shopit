using System.Text.Json.Serialization;

namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// The multi-tenancy side(s) a permission is available on, mirroring ABP's
/// <c>MultiTenancySides</c> option for permission definitions. Host-only permissions
/// (e.g. creating tenants) are only available to host-side principals and roles;
/// tenant-only permissions only to tenant-side ones; <see cref="Both"/> (the default)
/// is available everywhere. Availability is enforced when seeding grants, when
/// saving role permissions, in the permission matrix, and when resolving a user's
/// effective permissions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PermissionMultiTenancySide
{
    /// <summary>Available on both the host and tenant sides (default).</summary>
    Both = 0,

    /// <summary>Available only on the host side (system-wide operations).</summary>
    Host = 1,

    /// <summary>Available only on the tenant side.</summary>
    Tenant = 2,
}

/// <summary>Helpers for <see cref="PermissionMultiTenancySide"/>.</summary>
public static class PermissionMultiTenancySideExtensions
{
    /// <summary>
    /// Whether a permission with this declared side is available when the current
    /// context is <paramref name="currentSide"/> (host or tenant).
    /// </summary>
    public static bool IsAvailableOn(
        this PermissionMultiTenancySide side,
        PermissionMultiTenancySide currentSide)
        => side == PermissionMultiTenancySide.Both || side == currentSide;
}
