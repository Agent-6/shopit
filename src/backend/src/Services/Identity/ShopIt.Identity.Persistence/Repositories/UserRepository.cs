using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Persistence.Repositories;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Repositories;

public class UserRepository(
    ApplicationDbContext dbContext,
    ILookupNormalizer lookupNormalizer,
    ILogger<UserRepository> logger) : Repository<User, Guid, ApplicationDbContext>(dbContext), IUserRepository
{
    private readonly ILookupNormalizer _lookupNormalizer = lookupNormalizer;
    private readonly ILogger<UserRepository> _logger = logger;

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <c>IgnoreQueryFilters()</c> is deliberate here, and it is the narrowest form of it: one read, in
    /// the one flow that runs before a tenant is known (ADR-0001, <em>Legitimate escapes from the
    /// filter</em>). The alternative the ADR prefers — <c>Change(new TenantInfo(Guid.Empty, "Host"))</c> —
    /// is not used because it makes the <em>whole request</em> host-scoped, and host scope is the
    /// fail-open state: everything after it would read across tenants too. This bypasses the filter for
    /// exactly one query, and returns without a tenant claim ever being consulted.
    /// </para>
    /// <para>
    /// Normalisation matches what <c>UserManager</c> writes into <c>NormalizedUserName</c> /
    /// <c>NormalizedEmail</c>, so the lookup is case-insensitive the same way <c>UserManager.FindByNameAsync</c>
    /// is — it just is not tenant-filtered.
    /// </para>
    /// </remarks>
    public async Task<User?> FindForLoginAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = _lookupNormalizer.NormalizeName(identifier);
        var normalizedEmail = _lookupNormalizer.NormalizeEmail(identifier);

        // Two rows at most: enough to tell "one match" from "ambiguous" without materialising a
        // platform-wide list on a path an unauthenticated caller can reach.
        var matches = await DbSet
            .IgnoreQueryFilters()
            .Where(u => u.NormalizedUserName == normalizedUserName || u.NormalizedEmail == normalizedEmail)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count > 1)
        {
            // Identity's unique-email rule is enforced per tenant (its validator queries the filtered
            // set), so the same address in two tenants is possible. Signing in cannot then be made
            // unambiguous, and guessing would cross a tenant boundary — so it is refused. The identifier
            // itself is not logged: it is a personal identifier, and the count is enough to diagnose this.
            _logger.LogWarning(
                "Refused a login lookup: the identifier matches {MatchCount} accounts in different tenants.",
                matches.Count);
        }

        return null;
    }

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
