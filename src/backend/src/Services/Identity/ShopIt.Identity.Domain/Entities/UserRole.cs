using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;
namespace ShopIt.Identity.Domain.Entities;

public class UserRole : IdentityUserRole<Guid>, IEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    // Public parameterless constructor for Identity
    public UserRole() : base() { }

    public object GetId() => new { UserId, RoleId };

    internal static UserRole Create(User user, Role role)
    {
        var userRole = new UserRole()
        {
            UserId = user.Id,
            RoleId = role.Id,
            TenantId = user.TenantId,
            AssignedAt = DateTime.UtcNow,
        };

        return userRole;
    }
}
