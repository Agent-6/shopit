using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Application.Caching;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Permissions;

/// <summary>
/// Removes cached permission snapshots when the grants they were derived from change.
/// </summary>
/// <remarks>
/// Explicit invalidation rather than expiry, because the writer always knows when it has changed
/// something. That is the reason the shared cache exists: Identity resolves once, every service reads
/// the same entry, and this removes it so no reader keeps serving the old value.
/// </remarks>
public interface IPermissionCacheInvalidator
{
    /// <summary>
    /// Invalidates the cached permissions of a single user, in the tenant that user belongs to.
    /// </summary>
    /// <remarks>
    /// Takes the user rather than an id on purpose: the cache entry is keyed by tenant <em>and</em> user, so
    /// removing the wrong tenant's entry would leave the stale one behind, and the entity's own
    /// <see cref="User.TenantId"/> is the authority on which tenant that is.
    /// </remarks>
    Task InvalidateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates every user holding the role, for a change to the role's own permissions.
    /// </summary>
    Task InvalidateRoleAsync(string roleName, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IPermissionCacheInvalidator" />
public class PermissionCacheInvalidator(
    UserManager<User> userManager,
    ICache cache,
    ILogger<PermissionCacheInvalidator> logger) : IPermissionCacheInvalidator
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICache _cache = cache;
    private readonly ILogger<PermissionCacheInvalidator> _logger = logger;

    public async Task InvalidateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.UserPermissions(user.Id, user.TenantId), cancellationToken);
    }

    /// <remarks>
    /// Editing a role's permissions changes the effective permissions of <em>every</em> user holding it,
    /// so this fans out. It is bounded by the role's membership and happens only when an administrator
    /// edits a role — as opposed to a published catalog, which is handled by the flag rather than by
    /// invalidation.
    /// </remarks>
    public async Task InvalidateRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);

        foreach (var user in users)
        {
            await _cache.RemoveAsync(CacheKeys.UserPermissions(user.Id, user.TenantId), cancellationToken);
        }

        _logger.LogInformation(
            "Invalidated cached permissions for {UserCount} user(s) holding role '{RoleName}'.",
            users.Count, roleName);
    }
}
