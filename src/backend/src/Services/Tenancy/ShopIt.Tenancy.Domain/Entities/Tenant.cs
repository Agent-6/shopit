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
    /// Gets the unique string identifier (slug/subdomain) for the tenant.
    /// Always stored as lowercase alphanumeric characters and hyphens.
    /// </summary>
    public string Identifier { get; private set; } = default!;

    /// <summary>
    /// Gets the tenant-specific connection string. Null if using the shared database.
    /// </summary>
    public string? ConnectionString { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the tenant is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Private parameterless constructor for Entity Framework Core and JSON serialization.
    /// </summary>
    private Tenant() : base()
    {
    }

    /// <summary>
    /// Private constructor to enforce validation rules during creation.
    /// </summary>
    private Tenant(Guid id, string name, string identifier, string? connectionString, Guid createdBy) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty or whitespace.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Tenant name cannot exceed 100 characters.", nameof(name));

        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Tenant identifier cannot be empty or whitespace.", nameof(identifier));

        if (identifier.Length > 50)
            throw new ArgumentException("Tenant identifier cannot exceed 50 characters.", nameof(identifier));

        if (createdBy == Guid.Empty)
            throw new ArgumentException("CreatedBy cannot be an empty Guid.", nameof(createdBy));

        var cleanIdentifier = ValidateAndNormalizeIdentifier(identifier);

        Name = name.Trim();
        Identifier = cleanIdentifier;
        ConnectionString = string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();
        IsActive = true;
    }

    /// <summary>
    /// Factory method to create a new Tenant instance.
    /// </summary>
    /// <param name="id">The unique identifier of the Tenant.</param>
    /// <param name="name">The display name of the Tenant.</param>
    /// <param name="identifier">The unique identifier string/slug for the Tenant.</param>
    /// <param name="connectionString">The optional database connection string.</param>
    /// <param name="createdBy">The unique identifier of the user creating the Tenant.</param>
    /// <returns>A new Tenant instance.</returns>
    public static Tenant Create(Guid id, string name, string identifier, string? connectionString, Guid createdBy)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be an empty Guid.", nameof(id));

        var tenant = new Tenant(id, name, identifier, connectionString, createdBy);
        
        tenant.RaiseDomainEvent(new TenantCreatedDomainEvent(tenant));
        
        return tenant;
    }

    /// <summary>
    /// Updates the Tenant's basic details.
    /// </summary>
    /// <param name="name">The new display name of the Tenant.</param>
    /// <param name="connectionString">The optional database connection string.</param>
    /// <param name="updatedBy">The unique identifier of the user performing the update.</param>
    public void Update(string name, string? connectionString, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty or whitespace.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Tenant name cannot exceed 100 characters.", nameof(name));

        if (updatedBy == Guid.Empty)
            throw new ArgumentException("UpdatedBy cannot be an empty Guid.", nameof(updatedBy));

        Name = name.Trim();
        ConnectionString = string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();

        RaiseDomainEvent(new TenantUpdatedDomainEvent(this));
    }

    /// <summary>
    /// Updates the tenant-specific connection string.
    /// </summary>
    /// <param name="connectionString">The new connection string, or null if switching to shared DB.</param>
    /// <param name="updatedBy">The unique identifier of the user performing the update.</param>
    public void UpdateConnectionString(string? connectionString, Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
            throw new ArgumentException("UpdatedBy cannot be an empty Guid.", nameof(updatedBy));

        var newConnString = string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();
        if (ConnectionString == newConnString)
            return;

        ConnectionString = newConnString;

        RaiseDomainEvent(new TenantConnectionStringUpdatedDomainEvent(Id, ConnectionString, updatedBy));
    }

    /// <summary>
    /// Activates the Tenant.
    /// </summary>
    /// <param name="updatedBy">The unique identifier of the user performing the activation.</param>
    public void Activate(Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
            throw new ArgumentException("UpdatedBy cannot be an empty Guid.", nameof(updatedBy));

        if (IsActive)
            return;

        IsActive = true;

        RaiseDomainEvent(new TenantActivatedDomainEvent(Id, updatedBy));
    }

    /// <summary>
    /// Deactivates the Tenant.
    /// </summary>
    /// <param name="updatedBy">The unique identifier of the user performing the deactivation.</param>
    public void Deactivate(Guid updatedBy)
    {
        if (updatedBy == Guid.Empty)
            throw new ArgumentException("UpdatedBy cannot be an empty Guid.", nameof(updatedBy));

        if (!IsActive)
            return;

        IsActive = false;

        RaiseDomainEvent(new TenantDeactivatedDomainEvent(Id, updatedBy));
    }

    /// <summary>
    /// Validates and normalizes the tenant identifier to ensure it is in slug format.
    /// Only allows lowercase letters, digits, and hyphens. Cannot start or end with a hyphen.
    /// </summary>
    private static string ValidateAndNormalizeIdentifier(string identifier)
    {
        var clean = identifier.Trim().ToLowerInvariant();

        if (clean.StartsWith('-') || clean.EndsWith('-'))
        {
            throw new ArgumentException("Tenant identifier cannot start or end with a hyphen.", nameof(identifier));
        }

        foreach (var c in clean)
        {
            if (!char.IsLetterOrDigit(c) && c != '-')
            {
                throw new ArgumentException("Tenant identifier must contain only alphanumeric characters or hyphens.", nameof(identifier));
            }
        }

        return clean;
    }
}
