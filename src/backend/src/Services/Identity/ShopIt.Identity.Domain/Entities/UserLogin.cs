using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Domain.Entities;

public class UserLogin : IdentityUserLogin<Guid>, IEntity, ITenantEntity
{
    public Guid TenantId { get; private set; } = default!;

    // Public parameterless constructor for Identity
    public UserLogin() : base() { }

    public object GetId() => new { UserId, ProviderKey };

    internal static UserLogin Create(User user, UserLoginInfo loginInfo)
    {
        var userLogin = new UserLogin()
        {
            LoginProvider = loginInfo.ProviderKey,
            ProviderDisplayName = loginInfo.ProviderDisplayName,
            ProviderKey = loginInfo.ProviderKey,
            TenantId = user.TenantId,
            UserId = user.Id,
        };

        return userLogin;
    }
}
