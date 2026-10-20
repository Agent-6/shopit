using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Queries.GetUserRoles;

public class GetUserRolesQueryHandler(UserManager<User> userManager) : IQueryHandler<GetUserRolesQuery, Result<GetUserRolesResult, Error>>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<Result<GetUserRolesResult, Error>> HandleAsync(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(request.UserId);
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new GetUserRolesResult(user.Id, roles);
    }
}
