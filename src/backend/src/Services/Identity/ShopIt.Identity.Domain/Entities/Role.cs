using ShopIt.Framework.Domain.Entities;
using ShopIt.Identity.Domain.Events.RoleEvents;

namespace ShopIt.Identity.Domain.Entities;

public class Role : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public string? Description { get; private set; }
    public string ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString();
    public Guid? TenantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }

    private readonly List<RoleClaim> _roleClaims = [];
    public IReadOnlyCollection<RoleClaim> RoleClaims => _roleClaims.AsReadOnly();

    /// <inheritdoc/>
    private Role() : base() { }

    private Role(Guid id) : base(id) { }

    public static Role Create(Guid id, string name, Guid? tenantId, string createdBy, string? description = null)
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

        var claim = RoleClaim.Create(Guid.NewGuid(), this, claimType, claimValue);
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
