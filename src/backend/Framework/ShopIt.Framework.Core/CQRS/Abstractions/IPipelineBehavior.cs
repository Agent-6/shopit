namespace ShopIt.Framework.Core.CQRS.Abstractions;

/// <summary>
/// Defines a pipeline behavior for handling requests and responses.
/// This interface allows you to implement cross-cutting concerns such as logging, validation, or caching around the execution of request handlers.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned from the handler.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request and optionally invokes the next behavior or handler in the pipeline.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">The delegate to invoke the next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The response from the handler or subsequent behaviors in the pipeline.</returns>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
