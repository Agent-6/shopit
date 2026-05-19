using System.Text.Json.Serialization;

namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Base class for audited entities.
/// Provides properties for managing creation, modification, and deletion audit info.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class AuditedEntity<TId> : Entity<TId>, IAuditedEntity<TId>
    where TId : notnull
{
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }
    public Guid? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// Private constructor for Entity Framework Core and JSON serialization.
    /// </summary>
    [JsonConstructor]
    protected AuditedEntity() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the AuditedEntity class with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    protected AuditedEntity(TId id) : base(id)
    {
    }
}
