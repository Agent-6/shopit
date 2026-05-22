using ShopIt.Framework.Domain.Repositories;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Domain.Repositories;

/// <summary>
/// Defines the repository contract for the Tenant aggregate.
/// </summary>
public interface ITenantRepository : IRepository<Tenant, Guid>
{
    /// <summary>
    /// Checks if a tenant with the specified display name already exists.
    /// </summary>
    /// <param name="name">The tenant display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if it exists, otherwise false.</returns>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of tenants, optionally filtered.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="filter">Optional search filter string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of tenants in the page and the total count matching the criteria.</returns>
    Task<(IEnumerable<Tenant> Tenants, long TotalCount, long TotalPages)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? filter, 
        CancellationToken cancellationToken = default);
}
