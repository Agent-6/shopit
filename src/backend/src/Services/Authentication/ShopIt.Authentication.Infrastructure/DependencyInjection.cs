using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Refit;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Authentication.Persistence.Data;
using ShopIt.Framework.Infrastructure;
using ShopIt.Identity.Application.Contracts.Clients;
using ShopIt.Identity.Application.Contracts.Implementations;
using ShopIt.Identity.Application.Contracts.Services;

namespace ShopIt.Authentication.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructureServices(configuration);

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "sso_cookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
            });

        services.AddAuthorization();

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        // In-memory dev stores used by the event-driven account flows.
        services.AddSingleton<IMockEmailService, MockEmailService>();
        services.AddSingleton<IFlowStatusStore, FlowStatusStore>();

        services.AddTransient<IdentityModelTokenHandler>();

        // Register Refit client for Identity service with auth handler
        services.AddRefitGeneratedClient<IIdentityApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new("https+http://identity-api"))
            .AddHttpMessageHandler<IdentityModelTokenHandler>();
        services.AddScoped<IIdentityServiceClient, IdentityServiceClient>();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>()
                       .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                options.SetIssuer("https://localhost:7234/");
                options.SetAuthorizationEndpointUris("connect/authorize")
                       .SetTokenEndpointUris("connect/token")
                       .SetUserInfoEndpointUris("connect/userinfo")
                       .SetEndSessionEndpointUris("connect/logout")
                       .SetIntrospectionEndpointUris("connect/introspect")
                       .SetRevocationEndpointUris("connect/revoke");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange()
                       .AllowRefreshTokenFlow()
                       .AllowClientCredentialsFlow();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Scopes.OfflineAccess);

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
                       .SetRefreshTokenLifetime(TimeSpan.FromDays(14))
                       .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(5));

                // TODO: remove in non-dev
                options.DisableAccessTokenEncryption();

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableStatusCodePagesIntegration();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
