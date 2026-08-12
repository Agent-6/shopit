using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Queries.GetMyPermissions;

public class GetMyPermissionsQueryHandler(
    ICurrentUser currentUser,
    UserManager<User> userManager,
    IPermissionResolver permissionResolver) : IQueryHandler<GetMyPermissionsQuery, GetMyPermissionsResult>
{
    public async Task<GetMyPermissionsResult> HandleAsync(GetMyPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Id is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await userManager.FindByIdAsync(currentUser.Id.Value.ToString());
        if (user is null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var permissions = await permissionResolver.GetGrantedPermissionsAsync(user, cancellationToken);
        return new GetMyPermissionsResult(permissions);
    }
}
