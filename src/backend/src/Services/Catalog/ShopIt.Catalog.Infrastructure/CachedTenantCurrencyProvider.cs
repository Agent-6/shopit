using Microsoft.Extensions.Caching.Memory;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Client.Services;

namespace ShopIt.Catalog.Infrastructure;

/// <summary>
/// <see cref="ITenantCurrencyProvider"/> with a short-lived, in-process cache in front of the tenant registry.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this cache and not the shared one.</strong> The shared <c>ICache</c> (FusionCache over Redis) has
/// fail-safe switched on platform-wide: when the registry cannot be reached it serves the last value it knew for up
/// to two hours instead of failing. On this path that would mean silently stamping a new price with a currency
/// nobody confirmed — which is the exact fallback #84 rejects — so this uses a private <see cref="IMemoryCache"/>
/// entry with an explicit sixty-second lifetime, where an unanswered lookup is a failure and nothing is cached.
/// </para>
/// <para>
/// <strong>The cost of that choice, stated.</strong> A currency a tenant changes through the Tenancy API is observed
/// by this service within one lifetime (60 seconds), not instantly, and a second instance of the catalog has its own
/// copy. Both are bounded and documented rather than hidden: sixty seconds of an old currency on new writes, and
/// existing prices keep their stored currency regardless (ADR-0006).
/// </para>
/// <para>
/// Only successes are cached. A registry that is down is retried on the next request, so an outage does not turn
/// into a fixed window of refused writes after it has ended.
/// </para>
/// </remarks>
public sealed class CachedTenantCurrencyProvider(
    ITenantCurrencyProvider inner,
    IMemoryCache cache) : ITenantCurrencyProvider
{
    /// <summary>
    /// How long a resolved currency is reused before the registry is asked again.
    /// </summary>
    /// <remarks>
    /// Sixty seconds: short enough that switching a tenant's currency is effectively immediate for a human, long
    /// enough that a burst of product writes costs one round trip instead of one per request.
    /// </remarks>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    private readonly ITenantCurrencyProvider _inner = inner;
    private readonly IMemoryCache _cache = cache;

    /// <inheritdoc />
    public async Task<Result<string, Error>> GetCurrencyAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(Key(tenantId), out string? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _inner.GetCurrencyAsync(tenantId, cancellationToken);

        // Failures are returned, never stored: see the remarks above.
        if (result.IsSuccess)
        {
            _cache.Set(Key(tenantId), result.Value, Lifetime);
        }

        return result;
    }

    /// <summary>The cache key for one tenant's currency. Keyed by tenant, which is what makes a hit safe.</summary>
    private static string Key(Guid tenantId) => $"catalog:tenant-currency:{tenantId:N}";
}
