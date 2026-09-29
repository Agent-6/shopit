namespace ShopIt.Framework.Application.Caching;

/// <summary>
/// Well-known cache keys. Centralised because a shared cache is only useful when every participant
/// agrees on the key — a typo on one side silently produces two caches instead of one.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// The resolved effective permissions of a user: direct claims plus role claims, already filtered
    /// by multi-tenancy side.
    /// </summary>
    /// <remarks>
    /// Written by Identity when it resolves permissions, read by any service that needs to authorise a
    /// caller, and invalidated by Identity when a permission grant changes.
    /// </remarks>
    public static string UserPermissions(Guid userId) => $"permissions:user:{userId:N}";
}
