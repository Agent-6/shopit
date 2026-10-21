using System.Net;
using Microsoft.Extensions.Logging;
using Refit;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Client.Clients;
using ShopIt.Tenancy.Client.Models;
using ShopIt.Tenancy.Client.Services;

namespace ShopIt.Tenancy.Client.Implementations;

/// <summary>
/// <see cref="ITenantCurrencyProvider"/> over Tenancy's internal endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This maps one HTTP answer to one outcome and does no caching — a cache is a caller-side tolerance for a stale
/// answer, so each service decides it for itself (the catalog's Infrastructure does, and says why).
/// </para>
/// <para>
/// <c>404</c> is translated into <see cref="TenantCurrencyErrors.TenantNotFound"/> rather than surfaced as a
/// transport error, because "the registry does not know this tenant" is an answer, and a write must be able to say
/// so instead of 500ing.
/// </para>
/// </remarks>
public class TenancyCurrencyClient(ITenancyApi tenancyApi, ILogger<TenancyCurrencyClient> logger)
    : ITenantCurrencyProvider
{
    private readonly ITenancyApi _tenancyApi = tenancyApi;
    private readonly ILogger<TenancyCurrencyClient> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<string, Error>> GetCurrencyAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ApiResponse<TenantCurrencyResponse> response;

        try
        {
            response = await _tenancyApi.GetTenantCurrencyAsync(tenantId, cancellationToken);
        }
        catch (Exception exception) when ((exception is HttpRequestException or TaskCanceledException)
                                           && !cancellationToken.IsCancellationRequested)
        {
            // No answer at all: the registry is down, unroutable, or timed out. Logged here because this is where
            // the detail exists; the caller gets a value it can map to a status code.
            _logger.LogError(
                exception,
                "Resolving the currency of tenant {TenantId} failed: the tenant registry could not be reached.",
                tenantId);

            return TenantCurrencyErrors.Unavailable(tenantId);
        }

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return TenantCurrencyErrors.TenantNotFound(tenantId);
        }

        if (!response.IsSuccessful || response.Content is null)
        {
            _logger.LogError(
                "Resolving the currency of tenant {TenantId} failed with {StatusCode}.",
                tenantId, response.StatusCode);

            return TenantCurrencyErrors.Unavailable(tenantId);
        }

        var currency = response.Content.Currency;

        // The registry validates what it stores, so a bad code here means the two sides disagree about the
        // contract. Refused rather than trusted: this value ends up on a price.
        if (!IsCurrencyCode(currency))
        {
            return TenantCurrencyErrors.InvalidResponse(tenantId, currency);
        }

        return currency.ToUpperInvariant();
    }

    /// <summary>
    /// Whether <paramref name="currency"/> has the shape of an ISO 4217 code: three ASCII letters.
    /// </summary>
    /// <remarks>
    /// Duplicated from the catalog's <c>Money</c> on purpose: the client must not depend on the domain of the
    /// service that happens to use it, and this is a two-line wire check, not a rule anyone will need to change.
    /// </remarks>
    private static bool IsCurrencyCode(string? currency) =>
        currency is { Length: 3 } && currency.All(char.IsAsciiLetter);
}
