using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Queries.GetUserRoles;

public class GetUserRolesQueryHandler(UserManager<User> userManager) : IQueryHandler<GetUserRolesQuery, GetUserRolesResult>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<GetUserRolesResult> HandleAsync(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);

        return new GetUserRolesResult(user.Id, roles);
    }
}
