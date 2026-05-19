using System.Text.Json.Serialization;

namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Base class for audited aggregate roots.
/// Provides properties for managing creation, modification, and deletion audit info.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public abstract class AuditedAggregateRoot<TId> : AggregateRoot<TId>, IAuditedEntity<TId>
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
    protected AuditedAggregateRoot() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the AuditedAggregateRoot class with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the aggregate root.</param>
    protected AuditedAggregateRoot(TId id) : base(id)
    {
    }
}
