using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Presentation;

namespace ShopIt.Catalog.Presentation;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the presentation layer: every <c>EndpointsModule</c> in this assembly, which the
    /// composition root maps through the framework's <c>MapEndpoints</c>.
    /// </summary>
    /// <remarks>
    /// There are no endpoint modules yet — product and category endpoints arrive with #77/#78. The
    /// registration stays because it is the seam those endpoints are added through, and because the
    /// service's HTTP surface is part of the scaffold even while it is empty: <c>/health</c> responds
    /// from the framework defaults, which is what the AppHost health check depends on.
    /// </remarks>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPresentationServices(configuration, typeof(DependencyInjection).Assembly);
        return services;
    }
}
