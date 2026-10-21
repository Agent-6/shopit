using ShopIt.Framework.Application.Caching;
using ShopIt.Identity.Client.Clients;
using ShopIt.Identity.Client.Models;
using ShopIt.Identity.Client.Services;

namespace ShopIt.Identity.Client.Implementations;

/// <inheritdoc cref="IIdentityPermissionClient" />
public sealed class CachedIdentityPermissionClient(
    IIdentityApi identityApi,
    ICache cache) : IIdentityPermissionClient
{
    public async Task<UserPermissionsSnapshot> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // One entry holding both the flag and the explicit set, under the key Identity also writes, so a
        // hit costs no HTTP call at all and an invalidation in Identity reaches this process through the
        // shared backplane rather than after a local expiry. The tenant is in the key because the answer
        // depends on it: the same user resolved in another tenant is a different set of role grants.
        return await cache.GetOrCreateAsync(
            CacheKeys.UserPermissions(userId, tenantId),
            async token => await FetchAsync(userId, tenantId, token),
            cancellationToken);
    }

    private async Task<UserPermissionsSnapshot> FetchAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var response = await identityApi.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

        if (!response.IsSuccessful || response.Content is null)
        {
            // Unknown user, or Identity unavailable. Cached as "nothing granted" so an unknown user does
            // not cause a call on every request.
            return UserPermissionsSnapshot.None;
        }

        return new UserPermissionsSnapshot(
            response.Content.IsAllPermissions,
            [.. response.Content.Permissions]);
    }
}
