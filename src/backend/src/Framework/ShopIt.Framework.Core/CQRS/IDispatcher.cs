using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Framework.Core.CQRS;

public interface IDispatcher
{
    /// <summary>
    /// Executes a query and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result that will be returned when the query is executed.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result of the query execution.</returns>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result that will be returned when the command is executed.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result of the command execution.</returns>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
