using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Users;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Queries.GetMyPermissions;

public class GetMyPermissionsQueryHandler(
    ICurrentUser currentUser,
    UserManager<User> userManager,
    IPermissionResolver permissionResolver) : IQueryHandler<GetMyPermissionsQuery, Result<GetMyPermissionsResult, Error>>
{
    public async Task<Result<GetMyPermissionsResult, Error>> HandleAsync(GetMyPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Id is null)
        {
            return UserErrors.Unauthenticated();
        }

        var user = await userManager.FindByIdAsync(currentUser.Id.Value.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(currentUser.Id.Value);
        }

        var permissions = await permissionResolver.GetGrantedPermissionsAsync(user, cancellationToken);
        return new GetMyPermissionsResult(permissions);
    }
}
