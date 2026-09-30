namespace ShopIt.Framework.Application.Caching;

/// <summary>
/// The resolved permissions of a user, as stored in the shared cache.
/// </summary>
/// <param name="IsAllPermissions">
/// Whether the user holds a role whose definition grants every permission. When true,
/// <paramref name="Permissions"/> is empty — holding everything is represented by the flag.
/// </param>
/// <param name="Permissions">
/// The permissions granted explicitly: direct claims plus role claims, filtered by multi-tenancy side.
/// </param>
/// <remarks>
/// <para>
/// This lives in the framework because it is a <em>cache contract</em>: Identity writes it when it
/// resolves permissions, and every service that authorises a caller reads it. Both sides must agree on
/// the shape, and a duplicated type kept in sync by property name alone would break silently.
/// </para>
/// <para>
/// The shape is deliberately catalog-independent. A published permission catalog only ever grants new
/// permissions to all-permissions roles, so neither field changes when a catalog is republished — which
/// is what stops a catalog publish from fanning out into an invalidation per cached user. Only a
/// genuine change to a user's own grants invalidates this entry.
/// </para>
/// </remarks>
public record UserPermissionsSnapshot(bool IsAllPermissions, string[] Permissions)
{
    /// <summary>An unknown or inactive user: nothing granted, and not an all-permissions holder.</summary>
    public static UserPermissionsSnapshot None { get; } = new(false, []);

    /// <summary>Whether the snapshot grants <paramref name="permissionName"/>.</summary>
    public bool Grants(string permissionName) =>
        IsAllPermissions
        || Array.Exists(Permissions, p => string.Equals(p, permissionName, StringComparison.OrdinalIgnoreCase));
}
