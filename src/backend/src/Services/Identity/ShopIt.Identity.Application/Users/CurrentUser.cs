using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users;

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
