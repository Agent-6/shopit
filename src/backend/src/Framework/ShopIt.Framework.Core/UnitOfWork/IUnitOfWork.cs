namespace ShopIt.Framework.Core.UnitOfWork;

public interface IUnitOfWork
{
    /// <summary>
    /// Begins a new transaction. If a transaction is already in progress, it will be reused for the current operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns></returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction. If no transaction is in progress, this method will throw an exception.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns></returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction. If no transaction is in progress, this method will throw an exception.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns></returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes made in the current transaction to the database context. If no transaction is in progress, this method will throw an exception.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <remarks>This method should be called after all operations within a transaction are completed, and before committing the transaction.</remarks>
    /// <returns>Number of entities affected by the changes.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
