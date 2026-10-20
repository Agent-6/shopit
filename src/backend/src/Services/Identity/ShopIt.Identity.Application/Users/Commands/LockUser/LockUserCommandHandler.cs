using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.LockUser;

public class LockUserCommandHandler(UserManager<User> userManager) : ICommandHandler<LockUserCommand, Result<LockUserResult, Error>>
{
    private static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(30);

    private readonly UserManager<User> _userManager = userManager;

    public async Task<Result<LockUserResult, Error>> HandleAsync(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(request.UserId);
        }

        var lockoutEnd = request.LockoutEnd ?? DateTimeOffset.UtcNow.Add(DefaultLockoutDuration);
        user.LockAccount(lockoutEnd);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return UserErrors.Invalid("User.Lock", result.Errors.Select(e => e.Description));
        }

        return new LockUserResult(user.Id, lockoutEnd);
    }
}
