using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.CQRS.Abstractions;

namespace ShopIt.Framework.Core.CQRS.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling request of type {RequestType}", typeof(TRequest).Name);

        var response = await next();

        _logger.LogInformation("Request of type {RequestType} completed with response of type {ResponseType}", typeof(TRequest).Name, typeof(TResponse).Name);

        return response;
    }
}
