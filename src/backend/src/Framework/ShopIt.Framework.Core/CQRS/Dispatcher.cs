using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core.CQRS.Abstractions;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Framework.Core.CQRS;

public class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private static readonly ConcurrentDictionary<Type, System.Reflection.MethodInfo> _executeMethodCache = new();

    private static readonly System.Reflection.MethodInfo _internalExecuteAsyncMethod =
        typeof(Dispatcher).GetMethod(nameof(InternalExecuteAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?? throw new InvalidOperationException(
            $"Could not find method {nameof(InternalExecuteAsync)} in type {typeof(Dispatcher).Name}");

    public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(query, cancellationToken);

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(command, cancellationToken);

    private async Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Get the actual type of the request
        var requestType = request.GetType();

        // Get the ExecuteAsync method through reflection
        var genericExecuteRequestMethod = _executeMethodCache.GetOrAdd(requestType, type =>
            _internalExecuteAsyncMethod.MakeGenericMethod(type, typeof(TResponse)));

        // Invoke the method and cast the result
        var task = (Task<TResponse>)genericExecuteRequestMethod.Invoke(this, [request, cancellationToken])!;

        return await task;
    }

    private async Task<TResponse> InternalExecuteAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        // Get the handler for the request
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        // Get the applicable pipeline behaviors for the request
        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Where(behavior => behavior is not null)
            .Reverse()
            .ToList();

        // Create the delegate that will execute the handler, this is the innermost delegate in the pipeline
        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
            handler.HandleAsync(request, cancellationToken);

        // Wrap the handler delegate with the pipeline behaviors
        foreach (var behavior in behaviors)
        {
            var next = handlerDelegate;
            handlerDelegate = () => behavior.HandleAsync(request, next, cancellationToken);
        };

        // Execute the pipeline, which will eventually call the handler and return the response
        return await handlerDelegate();
    }
}
