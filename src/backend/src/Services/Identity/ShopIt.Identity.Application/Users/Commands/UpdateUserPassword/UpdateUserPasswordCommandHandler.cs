using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPassword;

public class UpdateUserPasswordCommandHandler(UserManager<User> userManager) : ICommandHandler<UpdateUserPasswordCommand, UpdateUserPasswordResult>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<UpdateUserPasswordResult> HandleAsync(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        // Admin-initiated password change: drop any existing password then assign the new one.
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return new UpdateUserPasswordResult(user.Id, false, string.Join("; ", removeResult.Errors.Select(e => e.Description)));
        }

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
        {
            return new UpdateUserPasswordResult(user.Id, false, string.Join("; ", addResult.Errors.Select(e => e.Description)));
        }

        return new UpdateUserPasswordResult(user.Id, true, null);
    }
}
