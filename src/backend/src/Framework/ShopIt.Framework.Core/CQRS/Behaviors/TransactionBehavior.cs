using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.CQRS.Abstractions;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Core.UnitOfWork;

namespace ShopIt.Framework.Core.CQRS.Behaviors;

public class TransactionBehavior<TCommand, TResult>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TCommand, TResult>> logger)
    : IPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<TransactionBehavior<TCommand, TResult>> _logger = logger;

    public async Task<TResult> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var response = await next();

            await _unitOfWork.CommitAsync(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {Command}", typeof(TCommand).Name);
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
