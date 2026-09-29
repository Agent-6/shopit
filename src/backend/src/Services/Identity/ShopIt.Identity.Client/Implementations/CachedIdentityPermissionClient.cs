using ShopIt.Framework.Application.Caching;
using ShopIt.Identity.Client.Clients;
using ShopIt.Identity.Client.Services;

namespace ShopIt.Identity.Client.Implementations;

/// <inheritdoc cref="IIdentityPermissionClient" />
public sealed class CachedIdentityPermissionClient(
    IIdentityApi identityApi,
    ICache cache) : IIdentityPermissionClient
{
    private static readonly string[] NoPermissions = [];

    public async Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Cached as string[] rather than a set: the value is serialized into Redis, and
        // System.Text.Json cannot deserialize an interface type. The set is rebuilt here with the
        // ordinal-ignore-case comparer the rest of the permission code uses, which the serializer
        // would not preserve.
        var permissions = await cache.GetOrCreateAsync(
            CacheKeys.UserPermissions(userId),
            async token => await FetchAsync(userId, token),
            cancellationToken);

        return permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string[]> FetchAsync(Guid userId, CancellationToken cancellationToken)
    {
        var response = await identityApi.GetUserPermissionsAsync(userId, cancellationToken);

        if (!response.IsSuccessful || response.Content is null)
        {
            // Unknown user or Identity unavailable — no permissions rather than an exception. The
            // empty result is cached, so an unknown user does not cause a call on every request.
            return NoPermissions;
        }

        return [.. response.Content.Permissions];
    }
}
