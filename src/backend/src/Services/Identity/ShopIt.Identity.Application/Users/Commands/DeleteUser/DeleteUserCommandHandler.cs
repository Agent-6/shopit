using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, DeleteUserResult>
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

    public async Task<DeleteUserResult> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        // Either shape removes the user's access: a hard delete removes the row, a soft delete
        // deactivates it and the resolver treats an inactive user as holding nothing. Both make any
        // cached snapshot wrong.
        var result = request.Permanent
            ? await DeletePermanentlyAsync(user)
            : await SoftDeleteAsync(user);

        await _permissionCache.InvalidateUserAsync(user.Id, cancellationToken);

        return result;
    }

    private async Task<DeleteUserResult> DeletePermanentlyAsync(User user)
    {
        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
        return new DeleteUserResult(user.Id, true, "Permanent");
    }

    private async Task<DeleteUserResult> SoftDeleteAsync(User user)
    {
        user.Deactivate("soft-delete");
        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
        return new DeleteUserResult(user.Id, true, "Soft");
    }
}
