using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Tenancy.Application.Permissions;

namespace ShopIt.Tenancy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenancyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();

        // Named client used by ClientCredentialsTokenHandler to talk to the auth server.
        services.AddHttpClient("auth-server", client => client.BaseAddress = new("https+http://auth-api"));

        // Typed client that resolves permissions from the Identity service.
        services.AddTransient<ClientCredentialsTokenHandler>();
        services.AddHttpClient<ITenantPermissionClient, TenantPermissionClient>(client =>
                client.BaseAddress = new("https+http://identity-api"))
            .AddHttpMessageHandler<ClientCredentialsTokenHandler>();

        return services;
    }
}
