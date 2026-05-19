using ShopIt.Framework.Domain.Repositories;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Domain.Repositories;

/// <summary>
/// Defines the repository contract for the Tenant aggregate.
/// </summary>
public interface ITenantRepository : IRepository<Tenant, Guid>
{
    /// <summary>
    /// Gets a tenant by their unique identifier (slug/subdomain).
    /// </summary>
    /// <param name="identifier">The tenant identifier slug.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant if found, otherwise null.</returns>
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant with the specified identifier already exists.
    /// </summary>
    /// <param name="identifier">The tenant identifier slug.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if it exists, otherwise false.</returns>
    Task<bool> ExistsByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

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
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="filter">Optional search filter string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the list of tenants and the total count matching the criteria.</returns>
    Task<(IEnumerable<Tenant> Tenants, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? filter, 
        CancellationToken cancellationToken = default);
}
