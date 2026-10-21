using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using ShopIt.Identity.Client.Clients;
using ShopIt.Identity.Client.Implementations;
using ShopIt.Identity.Client.Services;
using ShopIt.Tenancy.Client.Clients;
using ShopIt.Tenancy.Client.Implementations;
using ShopIt.Tenancy.Client.Services;

namespace ShopIt.Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the catalog service's outbound adapters: client-credentials HTTP clients to Identity's and
    /// Tenancy's internal APIs, and the clients the use cases resolve through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both edges exist because the catalog owns neither the answer: enforcing <c>catalog.view</c> /
    /// <c>catalog.write</c> means asking Identity what the caller holds, and denominating a price means asking
    /// Tenancy what the tenant's currency is (ADR-0005, ADR-0006). The alternatives — reading grants or a currency
    /// from the catalog's own database — would each be a second source for a value another service owns, and the two
    /// would disagree the moment it changed.
    /// </para>
    /// <para>
    /// The currency lookup is cached here rather than in the client, because a cache is a decision about how stale an
    /// answer this service will tolerate, not part of the wire contract. See
    /// <see cref="CachedTenantCurrencyProvider"/> for which cache and why.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Used by ClientCredentialsTokenHandler to cache access tokens, and by the tenant-currency cache.
        services.AddMemoryCache();

        // Named client used by ClientCredentialsTokenHandler to talk to the auth server.
        services.AddHttpClient("auth-server", client => client.BaseAddress = new("https+http://auth-api"));

        // Typed client that resolves permissions from the Identity service.
        services.AddTransient<ClientCredentialsTokenHandler>();

        // Identity's internal API, called with client credentials.
        //
        // Generated client, not `AddRefitClient`: Refit 16 moved the reflection request builder into the
        // out-of-band `Refit.Reflection` package, so the reflection path throws NotSupportedException at
        // runtime unless that package is referenced — which turned every authorised Tenancy request into
        // a 500 (#54). Binding it the same way as Tenancy and Authentication keeps one behaviour.
        services.AddRefitGeneratedClient<IIdentityApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new("https+http://identity-api"))
            .AddHttpMessageHandler<ClientCredentialsTokenHandler>();

        // The tenant registry's internal API, for the currency a price must be denominated in. Same generated
        // client and same client-credentials handler as the Identity edge: Tenancy guards /api/internal with the
        // policy Identity uses, so a user token could not call it even if one were attached.
        services.AddRefitGeneratedClient<ITenancyApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new("https+http://tenancy-api"))
            .AddHttpMessageHandler<ClientCredentialsTokenHandler>();

        // Reads the shared cache before calling Identity, so the catalog service and Identity agree on
        // the resolved permissions and an invalidation in Identity reaches this process through the
        // backplane rather than after a local expiry.
        services.AddTransient<IIdentityPermissionClient, CachedIdentityPermissionClient>();

        // The transport, uncached: maps one HTTP answer to one outcome.
        services.AddTransient<TenancyCurrencyClient>();

        // What the use cases depend on: the transport behind a short-lived in-process cache, so a create or an
        // update does not pay for a round trip it could have predicted, and a failed lookup is never cached.
        services.AddTransient<ITenantCurrencyProvider>(provider => new CachedTenantCurrencyProvider(
            provider.GetRequiredService<TenancyCurrencyClient>(),
            provider.GetRequiredService<IMemoryCache>()));

        return services;
    }
}
