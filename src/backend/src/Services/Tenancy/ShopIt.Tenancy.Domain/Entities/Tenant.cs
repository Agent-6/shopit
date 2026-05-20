using ShopIt.Framework.Domain.Entities;
using ShopIt.Tenancy.Domain.Events;

namespace ShopIt.Tenancy.Domain.Entities;

/// <summary>
/// Represents a Tenant in the system.
/// Tenants are isolated organizational units with their own identifiers, name, and optional separate database connection strings.
/// </summary>
public class Tenant : AuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Gets the display name of the tenant.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Gets a value indicating whether the tenant is active.
    /// </summary>
    public bool IsActive { get; private set; } = false;

    /// <summary>
    /// Private parameterless constructor for Entity Framework Core and JSON serialization.
    /// </summary>
    private Tenant() : base()
    {
    }

    /// <summary>
    /// Private constructor to enforce validation rules during creation.
    /// </summary>
    private Tenant(Guid id, string name) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty or whitespace.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Tenant name cannot exceed 100 characters.", nameof(name));

        Name = name.Trim();
        IsActive = false;
    }

    /// <summary>
    /// Factory method to create a new Tenant instance.
    /// </summary>
    /// <param name="id">The unique identifier of the Tenant.</param>
    /// <param name="name">The display name of the Tenant.</param>
    /// <returns>A new Tenant instance.</returns>
    public static Tenant Create(Guid id, string name)
    {
        var tenant = new Tenant(id, name);
        
        tenant.RaiseDomainEvent(new TenantCreatedDomainEvent(tenant));
        
        return tenant;
    }

    /// <summary>
    /// Updates the Tenant's basic details.
    /// </summary>
    /// <param name="name">The new display name of the Tenant.</param>
    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty or whitespace.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Tenant name cannot exceed 100 characters.", nameof(name));

        Name = name.Trim();

        RaiseDomainEvent(new TenantUpdatedDomainEvent(this));
    }

    /// <summary>
    /// Activates the Tenant.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;

        RaiseDomainEvent(new TenantActivatedDomainEvent(Id));
    }

    /// <summary>
    /// Deactivates the Tenant.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;

        RaiseDomainEvent(new TenantDeactivatedDomainEvent(Id));
    }
}
