using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities;

public class UserClaim : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public string ClaimType { get; private set; }
    public string ClaimValue { get; private set; }
    public Guid? TenantId { get; private set; }

    private UserClaim() : base() { }

    private UserClaim(Guid id) : base(id) { }

    public static UserClaim Create(Guid id, User user, string claimType, string claimValue)
    {
        var userClaim = new UserClaim(id)
        {
            UserId = user.Id,
            User = user,
            ClaimType = claimType,
            ClaimValue = claimValue,
            TenantId = user.TenantId
        };

        return userClaim;
    }
}
