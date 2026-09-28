using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ShopIt.Framework.Domain.Users;

namespace ShopIt.Framework.Infrastructure.Users;

/// <summary>
/// Resolves the acting user from the current <see cref="HttpContext"/>'s principal.
/// </summary>
/// <remarks>
/// Returns <c>null</c> / <c>false</c> when there is no HTTP context or the request is anonymous —
/// see <see cref="ICurrentUser"/> for why that differs from <see cref="Tenancy.CurrentTenant"/>.
/// </remarks>
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? Id
    {
        get
        {
            var subject = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? Principal?.FindFirstValue("sub");

            return Guid.TryParse(subject, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.Identity?.Name;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? Principal?.FindFirstValue("email");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
