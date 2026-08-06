using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Core.CQRS.Abstractions;
using ShopIt.Framework.Core.CQRS.Behaviors;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Framework.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddRequestHandlers(assemblies);
        services.AddValidators(assemblies);
        return services;
    }

    private static IServiceCollection AddRequestHandlers(this IServiceCollection services, params Assembly[] assemblies)
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
                .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        // TODO: comment out for now, we will register behaviors manually until we add ordering support for pipeline behaviors
        // services.Scan(scan =>
        //     scan.FromAssemblyOf<IDispatcher>()
        //         .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehavior<,>)))
        //         .AsImplementedInterfaces()
        //         .WithScopedLifetime()
        // );
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddValidatorsFromAssemblies(assemblies);
        return services;
    }
}
