using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Permissions;

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
    Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(User user, CancellationToken cancellationToken = default);
}
