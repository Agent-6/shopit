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
            var f = filter.ToLowerInvariant();
            query = query.Where(u => u.UserName!.Contains(f, StringComparison.CurrentCultureIgnoreCase) || u.Email!.Contains(f, StringComparison.CurrentCultureIgnoreCase));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, total);
    }
}
