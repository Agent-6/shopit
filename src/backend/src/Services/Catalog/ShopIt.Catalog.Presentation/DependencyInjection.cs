using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Presentation;

namespace ShopIt.Catalog.Presentation;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the presentation layer: every <c>EndpointsModule</c> in this assembly (the categories and
    /// products modules), which the composition root maps through the framework's <c>MapEndpoints</c>.
    /// </summary>
    /// <remarks>
    /// The permission-authorization handler is registered separately, by
    /// <c>AddCatalogPermissionAuthorization</c>: it belongs to authorization wiring rather than to endpoint
    /// discovery, exactly as in Tenancy.
    /// </remarks>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPresentationServices(configuration, typeof(DependencyInjection).Assembly);
        return services;
    }
}
