using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Repositories;

namespace ShopIt.Framework.Persistence.Repositories;

/// <summary>
/// Base repository implementation using Entity Framework Core
/// </summary>
/// <typeparam name="TEntity">The type of the entity</typeparam>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
public class Repository<TEntity, TId, TDbContext> : IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TDbContext : DbContext
{
    protected readonly TDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(TDbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        DbSet = DbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await DbSet.FindAsync(new object[] { id }, cancellationToken);

        if (entity is null)
        {
            throw new EntityNotFoundException($"Entity of type {typeof(TEntity).Name} with ID {id} was not found.");
        }

        return entity;
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        DbSet.Remove(entity);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Exception thrown when an entity is not found
/// </summary>
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message) { }
}
