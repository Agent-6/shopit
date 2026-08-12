using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ShopIt.Tenancy.Infrastructure;

/// <summary>
/// Delegating handler that obtains a client-credentials access token from the authentication
/// server's <c>/connect/token</c> endpoint and attaches it as a Bearer token to outgoing
/// requests. Tokens are cached until near expiry to avoid repeated token requests.
/// </summary>
public class ClientCredentialsTokenHandler(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<ClientCredentialsTokenHandler> logger) : DelegatingHandler
{
    private const string ClientId = "shopit-backend";
    private const string ClientSecret = "BACKEND_SECRET";
    private const string Scope = "shopit-api";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "tenancy_client_credentials_token";

        // Reuse a cached token while it still has a comfortable margin before expiry.
        if (cache.TryGetValue(cacheKey, out CachedToken? cached)
            && cached is not null
            && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return cached.AccessToken;
        }

        logger.LogInformation("Requesting client credentials token from the authentication server");

        using var client = httpClientFactory.CreateClient("auth-server");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["scope"] = Scope,
        });

        var response = await client.PostAsync("/connect/token", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (payload?.AccessToken is null)
        {
            throw new InvalidOperationException("Failed to obtain a client credentials token from the authentication server.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, payload.ExpiresIn) - 30);
        cache.Set(cacheKey, new CachedToken(payload.AccessToken, expiresAt), expiresAt);

        return payload.AccessToken;
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);

    // The token endpoint returns OAuth2 snake_case keys (access_token, expires_in),
    // which default Web JSON options (camelCase/case-insensitive) cannot match across
    // underscores — without these attributes deserialization yields null/0 and the
    // handler reports a bogus "Failed to obtain" failure after a successful 2xx.
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
