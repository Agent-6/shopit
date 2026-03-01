using System.Text.Json.Serialization;

namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Base class for domain entities.
/// Provides built-in equality comparison based on entity Id.
/// Entities are considered equal if they have the same Id of the same type.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId> : IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Cache for the hash code to avoid recomputing.
    /// </summary>
    private int? _hashCode;

    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public TId Id { get; private set; }

    /// <summary>
    /// Private constructor for EF Core and JSON serialization.
    /// </summary>
    [JsonConstructor]
    protected Entity()
    {
        Id = default!;
    }

    /// <summary>
    /// Initializes a new instance of the Entity class.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided id is null.</exception>
    protected Entity(TId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// Two entities are equal if they are of the same type and have the same Id.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (IsTransient() || other.IsTransient())
            return false;

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether two entities are equal using the == operator.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are equal; otherwise, false.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two entities are not equal using the != operator.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// Returns a consistent hash code based on the entity's Id when not transient.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode()
    {
        if (IsTransient())
        {
            return base.GetHashCode();
        }

        if (!_hashCode.HasValue)
        {
            _hashCode = HashCode.Combine(GetType(), Id);
        }

        return _hashCode.Value;
    }

    /// <summary>
    /// Determines whether this entity is transient (has no Id assigned yet).
    /// </summary>
    /// <returns>true if the entity is transient; otherwise, false.</returns>
    private bool IsTransient()
    {
        return Id.Equals(default(TId));
    }
}
