using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Persistence.Repositories;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : Repository<User, Guid, ApplicationDbContext>(dbContext), IUserRepository
{
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(int page, int pageSize, string? filter, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(u => EF.Functions.ILike((u.UserName ?? string.Empty), $"%{filter}%") || EF.Functions.ILike((u.Email ?? string.Empty), $"%{filter}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, total);
    }

    public async Task<IDictionary<Guid, List<string>>> GetRoleNamesForUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, List<string>>();
        }

        var assignments = await (
            from ur in DbContext.Set<UserRole>()
            join r in DbContext.Set<Role>() on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name ?? string.Empty })
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).Distinct().ToList());
    }
}
