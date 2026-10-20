using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UnlockUser;

public class UnlockUserCommandHandler(UserManager<User> userManager) : ICommandHandler<UnlockUserCommand, Result<UnlockUserResult, Error>>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<Result<UnlockUserResult, Error>> HandleAsync(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(request.UserId);
        }

        user.UnlockAccount();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return UserErrors.Invalid("User.Unlock", result.Errors.Select(e => e.Description));
        }

        return new UnlockUserResult(user.Id, true);
    }
}
