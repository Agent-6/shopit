using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShopIt.Framework.Core.UnitOfWork;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Persistence.Events;

namespace ShopIt.Framework.Persistence.UnitOfWork;

public class UnitOfWork<TContext>(TContext context, IDomainEventDispatcher domainEventDispatcher) : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _context = context;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            return;

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        // 1. Persist domain state changes to the DB (still within the open transaction)
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Collect the domain events from tracked aggregate roots, clear them, and dispatch
        //    them. Handlers run INSIDE the transaction, so they can write additional rows (e.g.
        //    OutboxMessages) atomically with the domain changes.
        var domainEvents = await DomainEventDispatch.DispatchPendingAsync(_context, _domainEventDispatcher, cancellationToken);

        // 3. Persist changes made by domain event handlers. Handlers typically add OutboxMessage
        //    rows via IOutboxWriter (which only tracks them, it does not save) — without this second
        //    save the transaction commits with those rows left only in the ChangeTracker, and the
        //    integration event is silently lost.
        if (domainEvents.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (_transaction is null) throw new InvalidOperationException("No transaction to commit.");

        // 4. Commit the transaction — domain changes + outbox writes are now durable together
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) throw new InvalidOperationException("No transaction to rollback.");

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) throw new InvalidOperationException("No transaction to commit.");
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            return await action();
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
