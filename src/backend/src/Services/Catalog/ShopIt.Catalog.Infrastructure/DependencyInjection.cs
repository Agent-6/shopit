using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using ShopIt.Identity.Client.Clients;
using ShopIt.Identity.Client.Implementations;
using ShopIt.Identity.Client.Services;

namespace ShopIt.Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the catalog service's outbound adapter: a client-credentials HTTP client to Identity's
    /// internal API, and the permission client the authorization handler resolves through.
    /// </summary>
    /// <remarks>
    /// This mirrors <c>ShopIt.Tenancy.Infrastructure</c> because the dependency is the same one: enforcing
    /// <c>catalog.view</c> / <c>catalog.write</c> means asking Identity what the caller holds, which is an
    /// HTTP call, not a local lookup. The alternative — reading grants from the catalog's own database —
    /// would be a second permission source, and the two would disagree the moment a grant changed.
    /// </remarks>
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Used by ClientCredentialsTokenHandler to cache access tokens.
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

        // Reads the shared cache before calling Identity, so the catalog service and Identity agree on
        // the resolved permissions and an invalidation in Identity reaches this process through the
        // backplane rather than after a local expiry.
        services.AddTransient<IIdentityPermissionClient, CachedIdentityPermissionClient>();

        return services;
    }
}
