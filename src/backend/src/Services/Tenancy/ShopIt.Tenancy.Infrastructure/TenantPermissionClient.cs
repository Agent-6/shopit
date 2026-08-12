using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using ShopIt.Tenancy.Application.Permissions;

namespace ShopIt.Tenancy.Infrastructure;

/// <summary>
/// Resolves a user's effective permissions by calling the Identity service's internal
/// <c>/api/internal/users/{id}/permissions</c> endpoint (authenticated with client
/// credentials via <see cref="ClientCredentialsTokenHandler"/>). Results are cached
/// briefly since permissions change infrequently.
/// </summary>
public class TenantPermissionClient(
    HttpClient httpClient,
    IMemoryCache cache) : ITenantPermissionClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant_permissions_{userId}";

        if (cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var response = await httpClient.GetAsync($"/api/internal/users/{userId}/permissions", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // The user no longer exists — treat as having no permissions.
            var empty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            cache.Set(cacheKey, empty, CacheDuration);
            return empty;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PermissionsResponse>(cancellationToken);
        var permissions = payload?.Permissions is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : payload.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

        cache.Set(cacheKey, permissions, CacheDuration);
        return permissions;
    }

    private sealed record PermissionsResponse(IReadOnlyCollection<string>? Permissions);
}
