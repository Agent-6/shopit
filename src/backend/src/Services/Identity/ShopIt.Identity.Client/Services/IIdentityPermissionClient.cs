using ShopIt.Framework.Application.Caching;

namespace ShopIt.Identity.Client.Services;

/// <summary>
/// Resolves a user's effective permissions, reading the shared cache first and only calling Identity
/// when the entry is absent.
/// </summary>
/// <remarks>
/// <para>
/// Consumers depend on this rather than calling the endpoint directly, so the cache is consulted
/// <em>before</em> the HTTP hop. On a hit there is no call to Identity at all.
/// </para>
/// <para>
/// The entry lives under <c>CacheKeys.UserPermissions</c>, the same key Identity writes when it resolves
/// permissions and removes when a grant changes — so an invalidation in Identity is seen here through the
/// shared backplane rather than after a local expiry.
/// </para>
/// </remarks>
public interface IIdentityPermissionClient
{
    /// <summary>
    /// The permissions held by <paramref name="userId"/>, or <see cref="UserPermissionsSnapshot.None"/>
    /// when the user is unknown.
    /// </summary>
    Task<UserPermissionsSnapshot> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
