using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities;

public class UserLogin : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public string LoginProvider { get; private set; }
    public string ProviderKey { get; private set; }
    public string? ProviderDisplayName { get; private set; }
    public Guid? TenantId { get; private set; }

    private UserLogin() : base() { }

    // TODO: add non-generic Entity for multi-keyed entities
    private UserLogin(Guid id) : base(id) { }

    public static UserLogin Create(Guid id, User user, UserLoginInfo loginInfo)
    {
        var userLogin = new UserLogin(id)
        {
            LoginProvider = loginInfo.ProviderKey,
            ProviderDisplayName = loginInfo.ProviderDisplayName,
            ProviderKey = loginInfo.ProviderKey,
            TenantId = user.TenantId,
            User = user,
            UserId = user.Id,
        };

        return userLogin;
    }
}
