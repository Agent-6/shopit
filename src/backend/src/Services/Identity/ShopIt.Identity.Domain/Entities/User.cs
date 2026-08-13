using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Enums;
using ShopIt.Identity.Domain.Events.UserEvents;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Domain.Entities;

public class User : IdentityUser<Guid>, IAggregateRoot<Guid>, ITenantEntity
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

    // Multi-tenancy
    public Guid TenantId { get; private set; } = default!;

    // Custom domain properties
    public string? FirstName { get; private set; } = default!;
    public string? LastName { get; private set; } = default!;
    public string? ProfilePictureUrl { get; private set; } = default!;
    public DateTime? LastLoginAt { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public DateTime CreatedAt { get; private set; } = default!;
    public DateTime LastModifiedAt { get; private set; } = default!;
    public string CreatedBy { get; private set; } = default!;

    // Navigation properties
    private readonly List<UserRole> _userRoles = [];
    private readonly List<UserClaim> _claims = [];
    private readonly List<UserLogin> _logins = [];
    private readonly List<UserToken> _tokens = [];

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserClaim> Claims => _claims.AsReadOnly();
    public IReadOnlyCollection<UserLogin> Logins => _logins.AsReadOnly();
    public IReadOnlyCollection<UserToken> Tokens => _tokens.AsReadOnly();

    // Public parameterless constructor for Identity
    public User() : base() { }

    /// <summary>
    /// Initializes a new instance of the User class with the specified identifier.
    /// </summary>
    /// <param name="id">The Identifier of the User.</param>
    private User(Guid id) : base() => Id = id;

    public static User Create(Guid id, string email, string userName, Guid tenantId, string createdBy)
    {
        var user = new User(id)
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            TenantId = tenantId,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            Status = UserStatus.Active,
            IsActive = true
        };

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user));
        return user;
    }

    /// <summary>
    /// Creates a user via the admin invitation flow. The account starts in
    /// <see cref="UserStatus.PendingActivation"/> (inactive, email unverified) and can only
    /// sign in after the invited user completes activation with a valid token and password.
    /// </summary>
    /// <param name="activationToken">Time-limited, cryptographically signed activation token.</param>
    /// <param name="activationTokenExpiresAt">UTC expiry of <paramref name="activationToken"/>.</param>
    public static User Invite(
        Guid id,
        string email,
        string userName,
        Guid tenantId,
        string createdBy,
        string activationToken,
        DateTimeOffset activationTokenExpiresAt)
    {
        var user = new User(id)
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            TenantId = tenantId,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            Status = UserStatus.PendingActivation,
            IsActive = false
        };

        user.RaiseDomainEvent(new UserInvitedDomainEvent(user, activationToken, activationTokenExpiresAt));
        return user;
    }

    // Password management
    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        SecurityStamp = Guid.NewGuid().ToString();
        RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id, SecurityStamp));
    }

    // put in application/infra layer
    //public bool VerifyPassword(string password, IPasswordHasher<User> hasher)
    //{
    //    return hasher.VerifyHashedPassword(this, PasswordHash, password) != PasswordVerificationResult.Failed;
    //}

    // Email confirmation
    public void ConfirmEmail()
    {
        if (EmailConfirmed)
            return;

        EmailConfirmed = true;
        RaiseDomainEvent(new UserEmailConfirmedDomainEvent(Id, Email));
    }

    public void ChangeEmail(string newEmail)
    {
        if (Email == newEmail)
            return;

        Email = newEmail;
        NormalizedEmail = newEmail.ToUpperInvariant();
        EmailConfirmed = false;
        SecurityStamp = Guid.NewGuid().ToString();
        LastModifiedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserEmailChangedDomainEvent(Id, newEmail));
    }

    // Phone number
    public void SetPhoneNumber(string phoneNumber)
    {
        PhoneNumber = phoneNumber;
        PhoneNumberConfirmed = false;
    }

    public void ConfirmPhoneNumber()
    {
        PhoneNumberConfirmed = true;
    }

    // Two factor
    public void EnableTwoFactor()
    {
        TwoFactorEnabled = true;
        RaiseDomainEvent(new UserTwoFactorEnabledDomainEvent(Id));
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        RaiseDomainEvent(new UserTwoFactorDisabledDomainEvent(Id));
    }

    // Lockout
    public void IncrementAccessFailedCount()
    {
        AccessFailedCount++;

        if (LockoutEnabled && AccessFailedCount >= 5) // Max attempts
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(15); // 15 min lockout
            RaiseDomainEvent(new UserLockedOutDomainEvent(Id, LockoutEnd.Value));
        }
    }

    public void ResetAccessFailedCount()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    public void SetLockoutEnd(DateTime? lockoutEnd)
    {
        LockoutEnd = lockoutEnd;
        if (lockoutEnd == null)
        {
            AccessFailedCount = 0;
        }
    }

    /// <summary>
    /// Locks the account until the specified time (admin-initiated lockout).
    /// </summary>
    public void LockAccount(DateTimeOffset until)
    {
        if (LockoutEnd == until)
            return;

        LockoutEnd = until;
        LastModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserLockedOutDomainEvent(Id, until));
    }

    /// <summary>
    /// Unlocks the account and resets the failed access counter (admin-initiated).
    /// </summary>
    public void UnlockAccount()
    {
        if (LockoutEnd == null && AccessFailedCount == 0)
            return;

        LockoutEnd = null;
        AccessFailedCount = 0;
        LastModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserUnlockedDomainEvent(Id));
    }

    // Login tracking
    public void RecordLogin(string loginProvider)
    {
        LastLoginAt = DateTime.UtcNow;

        if (loginProvider != null)
        {
            RaiseDomainEvent(new UserLoggedInDomainEvent(Id, loginProvider));
        }
    }

    // Role management
    public void AddToRole(Role role)
    {
        if (_userRoles.Any(ur => ur.RoleId == role.Id))
            return;

        var userRole = UserRole.Create(this, role);
        _userRoles.Add(userRole);

        RaiseDomainEvent(new UserAddedToRoleDomainEvent(Id, role.Id, role.Name));
    }

    public void RemoveFromRole(Role role)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == role.Id);
        if (userRole == null)
            return;

        _userRoles.Remove(userRole);
        RaiseDomainEvent(new UserRemovedFromRoleDomainEvent(Id, role.Id, role.Name));
    }

    // Claim management
    public void AddClaim(string claimType, string claimValue)
    {
        if (_claims.Any(c => c.ClaimType == claimType && c.ClaimValue == claimValue))
            return;

        var claim = UserClaim.Create(this, claimType, claimValue);
        _claims.Add(claim);

        RaiseDomainEvent(new UserClaimAddedDomainEvent(Id, claimType, claimValue));
    }

    public void RemoveClaim(string claimType, string claimValue)
    {
        var claim = _claims.FirstOrDefault(c => c.ClaimType == claimType && c.ClaimValue == claimValue);
        if (claim == null)
            return;

        _claims.Remove(claim);
        RaiseDomainEvent(new UserClaimRemovedDomainEvent(Id, claimType, claimValue));
    }

    public void ReplaceClaim(string oldClaimType, string oldClaimValue, string newClaimType, string newClaimValue)
    {
        RemoveClaim(oldClaimType, oldClaimValue);
        AddClaim(newClaimType, newClaimValue);
    }

    // Token management
    public void SetToken(string loginProvider, string name, string value)
    {
        var token = _tokens.FirstOrDefault(t => t.LoginProvider == loginProvider && t.Name == name);
        if (token == null)
        {
            token = UserToken.Create(this, loginProvider, name, value);
            _tokens.Add(token);
        }
        else
        {
            token.Value = value;
        }
    }

    public void RemoveToken(string loginProvider, string name)
    {
        var token = _tokens.FirstOrDefault(t => t.LoginProvider == loginProvider && t.Name == name);
        if (token != null)
        {
            _tokens.Remove(token);
        }
    }

    public string? GetToken(string loginProvider, string name)
    {
        return _tokens.FirstOrDefault(t => t.LoginProvider == loginProvider && t.Name == name)?.Value;
    }

    // External login management
    public void AddLogin(UserLoginInfo loginInfo)
    {
        if (_logins.Any(l => l.LoginProvider == loginInfo.LoginProvider && l.ProviderKey == loginInfo.ProviderKey))
            return;

        var login = UserLogin.Create(this, loginInfo);
        _logins.Add(login);

        RaiseDomainEvent(new UserExternalLoginAddedDomainEvent(Id, loginInfo.LoginProvider, loginInfo.ProviderKey));
    }

    public void RemoveLogin(string loginProvider, string providerKey)
    {
        var login = _logins.FirstOrDefault(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
        if (login is null)
            return;

        _logins.Remove(login);
        RaiseDomainEvent(new UserExternalLoginRemovedDomainEvent(Id, loginProvider, providerKey));
    }

    // Status management
    public void Deactivate(string reason)
    {
        IsActive = false;
        Status = UserStatus.Inactive;
        LastModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserDeactivatedDomainEvent(Id, reason));
    }

    public void Activate()
    {
        IsActive = true;
        Status = UserStatus.Active;
        LastModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserActivatedDomainEvent(Id, Email ?? string.Empty));
    }

    /// <summary>
    /// Completes the invitation flow: verifies the email address and flips the account to
    /// <see cref="UserStatus.Active"/>. The password hash is set beforehand (e.g. via
    /// <c>UserManager.AddPasswordAsync</c>) so it is validated against the password policy.
    /// Only valid for users in <see cref="UserStatus.PendingActivation"/>.
    /// </summary>
    public void CompleteActivation()
    {
        EmailConfirmed = true;
        IsActive = true;
        Status = UserStatus.Active;
        LastModifiedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserActivatedDomainEvent(Id, Email ?? string.Empty));
    }

    public void Suspend(DateTime suspendedUntil, string reason)
    {
        Status = UserStatus.Suspended;
        LockoutEnd = suspendedUntil;
        RaiseDomainEvent(new UserSuspendedDomainEvent(Id, suspendedUntil, reason));
    }

    // Profile management
    public void UpdateProfile(string firstName, string lastName, string? profilePictureUrl = null)
    {
        FirstName = firstName;
        LastName = lastName;
        if (profilePictureUrl is not null)
            ProfilePictureUrl = profilePictureUrl;

        LastModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, firstName, lastName));
    }
}
