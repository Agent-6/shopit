using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities;

public class UserToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public string LoginProvider { get; private set; }
    public string Name { get; private set; }
    public string Value { get; private set; }
    public Guid? TenantId { get; private set; }

    private UserToken() : base() { }

    private UserToken(Guid id) : base(id) { }

    public static UserToken Create(Guid id, User user, string loginProvider, string name, string value)
    {
        var userToken = new UserToken(id)
        {
            UserId = id,
            User = user,
            LoginProvider = loginProvider,
            Name = name,
            Value = value,
            TenantId = user.TenantId
        };

        return userToken;
    }

    internal void SetValue(string value)
    {
        Value = value;
    }
}
