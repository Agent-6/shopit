using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Events.RoleEvents;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Domain.Entities;

public class Role : IdentityRole<Guid>, IAggregateRoot<Guid>, ITenantEntity
{
    #region IAggregateRoot Implementation

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    #endregion

    public Guid TenantId { get; private set; } = default!;
    public string? Description { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = default!;
    public string CreatedBy { get; private set; } = default!;


    private readonly List<RoleClaim> _roleClaims = [];
    public IReadOnlyCollection<RoleClaim> RoleClaims => _roleClaims.AsReadOnly();

    // Public parameterless constructor for Identity
    public Role() : base() { }

    private Role(Guid id) : base()
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        Id = id;
    }

    public static Role Create(Guid id, string name, Guid tenantId, string createdBy, string? description = null)
    {
        var role = new Role(id)
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            TenantId = tenantId,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        role.RaiseDomainEvent(new RoleCreatedDomainEvent(role));
        return role;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        ConcurrencyStamp = Guid.NewGuid().ToString(); // TODO: Consider using a more robust concurrency control mechanism

        RaiseDomainEvent(new RoleUpdatedDomainEvent(Id, name, description));
    }

    public void AddClaim(string claimType, string claimValue)
    {
        if (_roleClaims.Any(rc => rc.ClaimType == claimType && rc.ClaimValue == claimValue))
            return;

        var claim = RoleClaim.Create(this, claimType, claimValue);
        _roleClaims.Add(claim);

        RaiseDomainEvent(new RoleClaimAddedDomainEvent(Id, claimType, claimValue));
    }

    public void RemoveClaim(string claimType, string claimValue)
    {
        var claim = _roleClaims.FirstOrDefault(rc => rc.ClaimType == claimType && rc.ClaimValue == claimValue);
        if (claim == null)
            return;

        _roleClaims.Remove(claim);
        RaiseDomainEvent(new RoleClaimRemovedDomainEvent(Id, claimType, claimValue));
    }
}
