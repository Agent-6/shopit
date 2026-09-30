using ShopIt.Framework.Application.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace ShopIt.Framework.Infrastructure.Caching;

/// <summary>
/// <see cref="ICache"/> over FusionCache: an in-process L1 in front of Redis as L2, with Redis also
/// acting as the <strong>backplane</strong>.
/// </summary>
/// <remarks>
/// <para>
/// The backplane is the reason FusionCache was chosen over <c>HybridCache</c>. Without one, removing a
/// key clears only the removing process's L1 and the shared L2 — every other process keeps serving its
/// own stale L1 entry until that entry expires. That would defeat the invalidation this cache exists to
/// provide. With the Redis backplane, a removal is broadcast and every participant drops the entry.
/// </para>
/// <para>
/// Fail-safe is enabled by configuration: if Redis becomes unreachable, reads fall back to the local
/// value rather than failing the request.
/// </para>
/// </remarks>
public sealed class FusionCacheAdapter(IFusionCache cache) : ICache
{
    private readonly IFusionCache _cache = cache;

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrSetAsync<T>(
            key,
            async (_, token) => await factory(token),
            token: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, token: cancellationToken);
    }
}
