using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Permissions;

/// <summary>
/// A user's effective permissions, split so that the part which does not depend on the permission
/// catalog is separable from the part which does.
/// </summary>
/// <param name="IsAllPermissions">
/// Whether the user holds a role whose definition grants every permission. When true,
/// <paramref name="Permissions"/> is empty — holding everything is represented by the flag, not by
/// enumerating the catalog.
/// </param>
/// <param name="Permissions">
/// The permissions granted explicitly: direct claims plus role claims, filtered by multi-tenancy side.
/// </param>
/// <remarks>
/// The split exists for caching. A published permission catalog only grants new permissions to
/// all-permissions roles, so neither of these two values changes when a catalog is republished — which
/// is what stops a catalog publish from turning into an invalidation fan-out across every cached user.
/// </remarks>
public record EffectivePermissions(bool IsAllPermissions, IReadOnlySet<string> Permissions);

/// <summary>
/// Computes the effective set of granted permission names for a user.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>
    /// Returns the effective permissions for <paramref name="user"/>: direct user claims plus
    /// claims from every role the user belongs to. Tenant-scoped resolution falls back to host
    /// scope so system (host) roles assigned to tenant users are still honored.
    /// </summary>
    /// <remarks>
    /// Materialises the whole catalog when the user holds an all-permissions role. Prefer
    /// <see cref="GetEffectivePermissionsAsync"/> where the caller only needs to test membership — it
    /// avoids materialising, and it is the shape that caches well.
    /// </remarks>
    Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the flag and the explicitly granted set, without materialising the catalog for a user
    /// who holds an all-permissions role.
    /// </summary>
    Task<EffectivePermissions> GetEffectivePermissionsAsync(User user, CancellationToken cancellationToken = default);
}
