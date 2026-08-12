namespace ShopIt.Identity.Domain.Users;

/// <summary>
/// Provides access to the user making the current request, resolved from the
/// authenticated principal's claims (the <c>sub</c> claim in OpenIddict tokens).
/// </summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
