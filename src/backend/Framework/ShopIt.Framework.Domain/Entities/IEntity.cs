namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Represents a base interface for domain entities.
/// All domain entities should implement this interface to ensure consistency.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IEntity<out TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TId Id { get; }
}
