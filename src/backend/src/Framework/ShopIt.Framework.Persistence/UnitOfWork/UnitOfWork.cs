using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShopIt.Framework.Core.UnitOfWork;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Events;

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

        // 2. Collect all domain events from tracked aggregate roots, then clear them
        //    Domain event handlers run INSIDE the transaction, so they can write
        //    additional rows (e.g. OutboxMessages) atomically with the domain changes.
        var aggregates = _context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        // 3. Dispatch domain events (handlers may write to the outbox table, etc.)
        if (domainEvents.Count > 0)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

            // 3b. Persist changes made by domain event handlers. Handlers typically add
            //     OutboxMessage rows via IOutboxWriter (which only tracks them, it does not
            //     save) — without this second save the transaction commits with those rows
            //     left only in the ChangeTracker, and the integration event is silently lost.
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
