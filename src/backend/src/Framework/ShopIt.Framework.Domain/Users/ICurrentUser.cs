namespace ShopIt.Framework.Domain.Users;

/// <summary>
/// Provides access to the user making the current request, resolved from the
/// authenticated principal's claims (the <c>sub</c> claim in OpenIddict tokens).
/// </summary>
/// <remarks>
/// <para>
/// Deliberately returns <c>null</c> / <c>false</c> rather than throwing when there is no HTTP context
/// or the request is anonymous — unlike <see cref="Tenancy.ICurrentTenant"/>, which throws. An absent
/// tenant is a bug; an absent user is normal. That difference is intentional and should not be
/// "fixed" for consistency.
/// </para>
/// See <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>The acting user's id, or <c>null</c> when anonymous or unparseable.</summary>
    Guid? Id { get; }

    /// <summary>The acting user's name, or <c>null</c> when anonymous.</summary>
    string? UserName { get; }

    /// <summary>The acting user's email, or <c>null</c> when anonymous or not present.</summary>
    string? Email { get; }

    /// <summary>Whether the current principal is authenticated.</summary>
    bool IsAuthenticated { get; }
}
