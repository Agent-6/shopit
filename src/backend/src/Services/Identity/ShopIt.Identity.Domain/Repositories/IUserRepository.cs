using ShopIt.Framework.Domain.Repositories;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(int page, int pageSize, string? filter, CancellationToken cancellationToken = default);
    Task<IDictionary<Guid, List<string>>> GetRoleNamesForUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
