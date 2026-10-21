using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, Result<DeleteUserResult, Error>>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IPermissionCacheInvalidator _permissionCache;

    public DeleteUserCommandHandler(
        UserManager<User> userManager,
        IUserRepository userRepository,
        IPermissionCacheInvalidator permissionCache)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _permissionCache = permissionCache;
    }

    public async Task<Result<DeleteUserResult, Error>> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(request.UserId);
        }

        // Either shape removes the user's access: a hard delete removes the row, a soft delete
        // deactivates it and the resolver treats an inactive user as holding nothing. Both make any
        // cached snapshot wrong.
        var result = request.Permanent
            ? await DeletePermanentlyAsync(user)
            : await SoftDeleteAsync(user);

        await _permissionCache.InvalidateUserAsync(user, cancellationToken);

        return result;
    }

    private async Task<Result<DeleteUserResult, Error>> DeletePermanentlyAsync(User user)
    {
        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded)
        {
            return UserErrors.Invalid("User.Delete", res.Errors.Select(e => e.Description));
        }
        return new DeleteUserResult(user.Id, true, "Permanent");
    }

    private async Task<Result<DeleteUserResult, Error>> SoftDeleteAsync(User user)
    {
        user.Deactivate("soft-delete");
        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded)
        {
            return UserErrors.Invalid("User.Delete", res.Errors.Select(e => e.Description));
        }
        return new DeleteUserResult(user.Id, true, "Soft");
    }
}
