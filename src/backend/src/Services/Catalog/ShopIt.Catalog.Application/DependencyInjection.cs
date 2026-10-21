using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core;
using ShopIt.Catalog.Domain.Permissions;

namespace ShopIt.Catalog.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer: command/query handlers, validators and pipeline behaviours from
    /// this assembly, plus the types the service declares rather than implements.
    /// </summary>
    /// <remarks>
    /// There are no handlers yet — product and category use cases arrive with #77/#78 — so today this
    /// registers the permission provider and the framework's CQRS plumbing. The scan is unconditional
    /// because it is what every service does; a handler added to this assembly is picked up by
    /// convention, with no edit here.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplicationServices(typeof(DependencyInjection).Assembly);

        // This service's permission catalog (published to Identity at startup by the framework hook).
        services.AddSingleton<ShopItCatalogPermissionDefinitionProvider>();

        return services;
    }
}
