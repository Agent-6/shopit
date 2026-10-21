using ShopIt.Framework.Application.Caching;

namespace ShopIt.Identity.Client.Services;

/// <summary>
/// Resolves a user's effective permissions <em>in a tenant</em>, reading the shared cache first and only
/// calling Identity when the entry is absent.
/// </summary>
/// <remarks>
/// <para>
/// Consumers depend on this rather than calling the endpoint directly, so the cache is consulted
/// <em>before</em> the HTTP hop. On a hit there is no call to Identity at all.
/// </para>
/// <para>
/// The tenant is a required parameter because resolution is tenant-dependent: a user's role-derived grants
/// come from the copies of their roles that exist in that tenant. It is deliberately not read from ambient
/// state inside the implementation — the caller passes the tenant the request is acting as, so an entry can
/// never be keyed with a tenant that differs from the answer inside it.
/// </para>
/// <para>
/// The entry lives under <c>CacheKeys.UserPermissions(userId, tenantId)</c>, the same key Identity writes when
/// it resolves permissions and removes when a grant changes — so an invalidation in Identity is seen here
/// through the shared backplane rather than after a local expiry.
/// </para>
/// </remarks>
public interface IIdentityPermissionClient
{
    /// <summary>
    /// The permissions held by <paramref name="userId"/> in <paramref name="tenantId"/>, or
    /// <see cref="UserPermissionsSnapshot.None"/> when the user is unknown there.
    /// </summary>
    /// <param name="userId">The user whose permissions are needed.</param>
    /// <param name="tenantId">The tenant to resolve them in; <see cref="Guid.Empty"/> for host scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<UserPermissionsSnapshot> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
