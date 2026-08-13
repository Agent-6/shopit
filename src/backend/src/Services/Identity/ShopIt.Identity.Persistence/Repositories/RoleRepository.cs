using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Persistence.Repositories;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Repositories;

public class RoleRepository(ApplicationDbContext dbContext) : Repository<Role, Guid, ApplicationDbContext>(dbContext), IRoleRepository
{
    public async Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<(IEnumerable<Role> Roles, int TotalCount)> GetPagedAsync(int page, int pageSize, string? filter, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(r => EF.Functions.ILike((r.Name ?? string.Empty), $"%{filter}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (roles, total);
    }

    public async Task<int> CountUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<UserRole>().CountAsync(ur => ur.RoleId == roleId, cancellationToken);
}
