using ShopIt.Framework.Domain.Entities;
namespace ShopIt.Identity.Domain.Entities;

public class UserRole : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; }
    public Guid? TenantId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    private UserRole() { }

    private UserRole(Guid id) : base(id) { }

    public static UserRole Create(Guid id, User user, Role role)
    {
        var userRole = new UserRole(id)
        {
            UserId = user.Id,
            User = user,
            RoleId = role.Id,
            Role = role,
            TenantId = user.TenantId,
            AssignedAt = DateTime.UtcNow,
        };

        return userRole;
    }
}
