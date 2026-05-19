namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Defines a contract for entities that maintain creation, modification, and deletion audit trails.
/// </summary>
public interface IAuditedEntity : IEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last modified.
    /// </summary>
    DateTimeOffset? LastModifiedOn { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified the entity.
    /// </summary>
    Guid? LastModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was deleted.
    /// </summary>
    DateTimeOffset? DeletedOn { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the entity.
    /// </summary>
    Guid? DeletedBy { get; set; }
}

/// <summary>
/// Generic interface for audited entities.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IAuditedEntity<out TId> : IEntity<TId>, IAuditedEntity
    where TId : notnull
{
}
