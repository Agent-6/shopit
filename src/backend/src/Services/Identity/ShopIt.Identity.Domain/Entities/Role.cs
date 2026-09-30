using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Events.RoleEvents;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Identity.Domain.Entities;

public class Role : IdentityRole<Guid>, IAggregateRoot<Guid>, ITenantEntity
{
    #region IAggregateRoot Implementation

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent domainEvent)
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

    /// <summary>
    /// The multi-tenancy side(s) this role is available on. Built-in roles take the
    /// side declared on their <c>RoleDefinition</c>; runtime-created roles default to
    /// <see cref="PermissionMultiTenancySide.Both"/>. A role physically lives in one
    /// tenant, so the effective side is still that tenant's; the declared side gates
    /// where the role is provisioned and who may be assigned it.
    /// </summary>
    public PermissionMultiTenancySide MultiTenancySide { get; private set; } = PermissionMultiTenancySide.Both;

    /// <summary>
    /// Whether this role comes from a built-in <c>RoleDefinition</c> rather than being created by a user.
    /// </summary>
    /// <remarks>
    /// Persisted so that "this is the built-in Admin" is a fact about the row, not an inference from
    /// its name. The name is user-controllable — roles can be created and renamed — so resolving
    /// anything security-relevant from it would let a caller with <c>role.create</c> or
    /// <c>role.update</c> impersonate a built-in role.
    /// </remarks>
    public bool IsStatic { get; private set; }


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

    public static Role Create(
        Guid id,
        string name,
        Guid tenantId,
        string createdBy,
        string? description = null,
        PermissionMultiTenancySide multiTenancySide = PermissionMultiTenancySide.Both,
        bool isStatic = false)
    {
        var role = new Role(id)
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            TenantId = tenantId,
            Description = description,
            MultiTenancySide = multiTenancySide,
            IsStatic = isStatic,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        role.RaiseDomainEvent(new RoleCreatedDomainEvent(role));
        return role;
    }

    /// <summary>
    /// Marks this role as built-in. Idempotent, and called by seeding so that roles created before
    /// this flag existed are corrected on startup.
    /// </summary>
    public void MarkAsStatic() => IsStatic = true;

    public void Update(string name, string? description)
    {
        // A built-in role's name is its identity: the permission model and the role definitions
        // resolve against it. It is fixed here, in the domain, as well as being rejected earlier by
        // validation, so no other path can rename one.
        if (IsStatic && !string.Equals(Name, name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"'{Name}' is a built-in role and cannot be renamed.");
        }

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
