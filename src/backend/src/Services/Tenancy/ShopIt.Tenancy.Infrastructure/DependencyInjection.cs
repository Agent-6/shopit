using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using ShopIt.Identity.Client.Clients;
using ShopIt.Identity.Client.Implementations;
using ShopIt.Identity.Client.Services;

namespace ShopIt.Tenancy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenancyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Still used by ClientCredentialsTokenHandler to cache access tokens.
        services.AddMemoryCache();

        // Named client used by ClientCredentialsTokenHandler to talk to the auth server.
        services.AddHttpClient("auth-server", client => client.BaseAddress = new("https+http://auth-api"));

        // Typed client that resolves permissions from the Identity service.
        services.AddTransient<ClientCredentialsTokenHandler>();

        // Identity's internal API, called with client credentials.
        services.AddRefitClient<IIdentityApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new("https+http://identity-api"))
            .AddHttpMessageHandler<ClientCredentialsTokenHandler>();

        // Reads the shared cache before calling Identity, so this service and Identity agree on the
        // resolved permissions and an invalidation in Identity reaches this process through the
        // backplane. This replaced a private in-memory cache with its own one-minute TTL, which was a
        // second, divergent view of the same data.
        services.AddTransient<IIdentityPermissionClient, CachedIdentityPermissionClient>();

        return services;
    }
}
