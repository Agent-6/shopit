using ShopIt.Framework.Domain.Repositories;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Repositories;

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Role> Roles, int TotalCount)> GetPagedAsync(int page, int pageSize, string? filter, CancellationToken cancellationToken = default);
    Task<int> CountUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
