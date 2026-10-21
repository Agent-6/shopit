namespace ShopIt.Framework.Application.Caching;

/// <summary>
/// Well-known cache keys. Centralised because a shared cache is only useful when every participant
/// agrees on the key — a typo on one side silently produces two caches instead of one.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// The resolved permissions of a user <em>in a tenant</em>, as a <see cref="UserPermissionsSnapshot"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written by Identity when it resolves permissions, read by any service that needs to authorise a
    /// caller, and invalidated by Identity when the user's own grants change. Publishing a permission
    /// catalog does <em>not</em> invalidate it — see <see cref="UserPermissionsSnapshot"/>.
    /// </para>
    /// <para>
    /// <strong>The tenant is part of the key because the answer depends on it.</strong> A user's
    /// role-derived grants come from the copies of their roles that exist in that tenant (plus the host
    /// copies), so the same user id resolved for tenant A and for tenant B are two different answers. With
    /// the tenant absent, the second caller would be served the first tenant's entry — a cross-tenant
    /// authorisation answer, which is the one collision a shared cache must not be able to produce. Host
    /// scope (<see cref="Guid.Empty"/>) is a tenant value like any other and gets its own entry.
    /// </para>
    /// </remarks>
    /// <param name="userId">The user the snapshot describes.</param>
    /// <param name="tenantId">The tenant the snapshot was resolved for; <see cref="Guid.Empty"/> for host.</param>
    public static string UserPermissions(Guid userId, Guid tenantId) =>
        $"permissions:user:{tenantId:N}:{userId:N}";
}
