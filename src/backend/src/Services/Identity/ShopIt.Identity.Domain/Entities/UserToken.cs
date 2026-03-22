using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Domain.Entities;

public class UserToken : IdentityUserToken<Guid>, IEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    // Public parameterless constructor for Identity
    public UserToken() : base() { }

    public object GetId() => new { UserId, LoginProvider, Name };

    internal static UserToken Create(User user, string loginProvider, string name, string value)
    {
        var userToken = new UserToken()
        {
            UserId = user.Id,
            LoginProvider = loginProvider,
            Name = name,
            Value = value,
            TenantId = user.TenantId
        };

        return userToken;
    }
}
