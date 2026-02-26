using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core.CQRS.Abstractions;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Framework.Core.CQRS;

public static class DependencyInjection
{
    // TODO: rename to add application services
    public static IServiceCollection AddDispatcher(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        return services;
    }

    public static IServiceCollection AddRequestHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.Scan(scan =>
            scan.FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehavior<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

//      // TODO: comment out for now, we will register behaviors manually until we add ordering support for pipeline behaviors
        // services.Scan(scan =>
        //     scan.FromAssemblyOf<IDispatcher>()
        //         .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehavior<,>)))
        //         .AsImplementedInterfaces()
        //         .WithScopedLifetime()
        // );
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Behaviors.TransactionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Behaviors.ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Behaviors.LoggingBehavior<,>));

        return services;
    }
}
