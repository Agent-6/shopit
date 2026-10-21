using ShopIt.Framework.Domain.Repositories;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Domain.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the account a login identifier — a username or an email, in any case — refers to, looking
    /// across every tenant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one lookup in Identity that is deliberately tenant-blind, and the reason it is a
    /// separate method rather than a widening of <see cref="FindByUsernameAsync"/>: authentication
    /// happens <em>before</em> a tenant is known, so the identifier's tenant is the <em>result</em> of
    /// this query, not an input to it. Resolving it inside the caller's scope therefore cannot work —
    /// the caller is a service validating credentials, whose token carries no tenant claim at all.
    /// </para>
    /// <para>
    /// The escape from the tenant filter ADR-0001 normally forbids is legitimate here and is recorded in
    /// that ADR under <em>Legitimate escapes from the filter</em>. Unlike <c>IgnoreQueryFilters()</c> at
    /// a write site, nothing read here is written back under the ambient scope: the returned entity's
    /// <see cref="User.TenantId"/> is authoritative and is what the access token is minted from.
    /// </para>
    /// </remarks>
    /// <param name="identifier">The username or email the caller typed into the login form.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The matching user, or <c>null</c> when no account matches <em>or</em> when the identifier matches
    /// accounts in more than one tenant. Ambiguity is refused rather than resolved by picking one, because
    /// picking would sign the caller into a tenant they may not belong to.
    /// </returns>
    Task<User?> FindForLoginAsync(string identifier, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(int page, int pageSize, string? filter, CancellationToken cancellationToken = default);
    Task<IDictionary<Guid, List<string>>> GetRoleNamesForUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
