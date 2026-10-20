using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPassword;

public class UpdateUserPasswordCommandHandler(UserManager<User> userManager) : ICommandHandler<UpdateUserPasswordCommand, Result<UpdateUserPasswordResult, Error>>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<Result<UpdateUserPasswordResult, Error>> HandleAsync(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(request.UserId);
        }

        // Admin-initiated password change: drop any existing password then assign the new one.
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return UserErrors.Invalid("User.Password", removeResult.Errors.Select(e => e.Description));
        }

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
        {
            return UserErrors.Invalid("User.Password", addResult.Errors.Select(e => e.Description));
        }

        return new UpdateUserPasswordResult(user.Id, true, null);
    }
}
