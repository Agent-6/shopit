namespace ShopIt.Framework.Application.Caching;

/// <summary>
/// The application's cache. Services depend on this rather than on a concrete cache technology.
/// </summary>
/// <remarks>
/// <para>
/// Backed by <c>HybridCache</c>: an in-process L1 in front of a distributed L2 (Redis). The L2 is what
/// makes the cache <em>shared</em> — two instances of the same service, or two different services,
/// observe the same entries and an invalidation in one is seen by the others.
/// </para>
/// <para>
/// <see cref="GetOrCreateAsync{T}"/> is the expected call shape: read, and on a miss produce the value
/// and store it. It is safe to call concurrently — the factory is not run once per caller for a hot
/// miss.
/// </para>
/// </remarks>
public interface ICache
{
    /// <summary>
    /// Reads <paramref name="key"/>, and on a miss runs <paramref name="factory"/> to produce it.
    /// </summary>
    /// <param name="key">The cache key. Use <see cref="CacheKeys"/> for well-known keys.</param>
    /// <param name="factory">Produces the value on a miss.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes <paramref name="key"/> from both cache levels. Safe to call for a key that is not present.
    /// </summary>
    /// <remarks>
    /// Invalidation is explicit rather than time-based wherever the writer knows the value changed —
    /// which is the point of having a shared L2 at all.
    /// </remarks>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
