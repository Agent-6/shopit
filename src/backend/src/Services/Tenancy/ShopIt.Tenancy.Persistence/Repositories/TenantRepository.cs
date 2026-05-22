using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Persistence.Repositories;
using ShopIt.Tenancy.Domain.Entities;
using ShopIt.Tenancy.Domain.Repositories;
using ShopIt.Tenancy.Persistence.Data;

namespace ShopIt.Tenancy.Persistence.Repositories;

public class TenantRepository(TenancyDbContext dbContext) 
    : Repository<Tenant, Guid, TenancyDbContext>(dbContext), ITenantRepository
{
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<(IEnumerable<Tenant> Tenants, long TotalCount, long TotalPages)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? filter, 
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(t => EF.Functions.ILike(t.Name, $"%{filter}%"));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var totalPages = (totalCount / pageSize) + (totalCount % pageSize > 0 ? 1 : 0);

        var tenants = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (tenants, totalCount, totalPages);
    }
}
