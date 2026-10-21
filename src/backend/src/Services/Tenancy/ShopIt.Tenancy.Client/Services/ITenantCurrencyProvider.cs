using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Tenancy.Client.Services;

/// <summary>
/// Answers "which currency does this tenant price in?", which is the tenant registry's answer to give.
/// </summary>
/// <remarks>
/// <para>
/// A consumer depends on this rather than on the Refit interface so the lookup's <em>policy</em> — caching, and
/// what an unreachable registry means — lives in the consumer's infrastructure. A cache is a decision about the
/// caller's tolerance for a stale answer, not part of the wire contract (ADR-0006).
/// </para>
/// <para>
/// A failure is a value, not an exception: a caller that cannot learn the currency must refuse the write rather
/// than fall back to a platform-wide setting. That is the whole point of #84 — the currency is the tenant's, and a
/// guess is a price in the wrong money.
/// </para>
/// </remarks>
public interface ITenantCurrencyProvider
{
    /// <summary>
    /// The ISO 4217 code the tenant's new prices must be denominated in.
    /// </summary>
    /// <param name="tenantId">The tenant to resolve; never <see cref="Guid.Empty"/>, which is host scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The currency in upper case, or a failure: <see cref="TenantCurrencyErrors.TenantNotFound"/> when the
    /// registry does not know the tenant, and <see cref="TenantCurrencyErrors.Unavailable"/> when it could not be
    /// asked.
    /// </returns>
    Task<Result<string, Error>> GetCurrencyAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
