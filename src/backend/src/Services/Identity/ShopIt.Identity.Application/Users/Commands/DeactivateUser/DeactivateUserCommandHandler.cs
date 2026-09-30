using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.DeactivateUser;

public class DeactivateUserCommandHandler(
    UserManager<User> userManager,
    IPermissionCacheInvalidator permissionCache) : ICommandHandler<DeactivateUserCommand, DeactivateUserResult>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IPermissionCacheInvalidator _permissionCache = permissionCache;

    public async Task<DeactivateUserResult> HandleAsync(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        user.Deactivate(request.Reason ?? "Deactivated by administrator");

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to deactivate the user: {errors}");
        }

        // Whether a user is active decides whether they hold anything at all — the resolver treats an
        // inactive user as having nothing — so this invalidates their snapshot.
        await _permissionCache.InvalidateUserAsync(user.Id, cancellationToken);

        return new DeactivateUserResult(user.Id, user.IsActive);
    }
}
