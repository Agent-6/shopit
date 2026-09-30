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
    /// <summary>Invalidates the cached permissions of a single user.</summary>
    Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default);

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

    public async Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.UserPermissions(userId), cancellationToken);
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
            await _cache.RemoveAsync(CacheKeys.UserPermissions(user.Id), cancellationToken);
        }

        _logger.LogInformation(
            "Invalidated cached permissions for {UserCount} user(s) holding role '{RoleName}'.",
            users.Count, roleName);
    }
}
