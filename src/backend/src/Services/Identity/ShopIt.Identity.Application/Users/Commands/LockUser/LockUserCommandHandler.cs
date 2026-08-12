using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.LockUser;

public class LockUserCommandHandler(UserManager<User> userManager) : ICommandHandler<LockUserCommand, LockUserResult>
{
    private static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(30);

    private readonly UserManager<User> _userManager = userManager;

    public async Task<LockUserResult> HandleAsync(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var lockoutEnd = request.LockoutEnd ?? DateTimeOffset.UtcNow.Add(DefaultLockoutDuration);
        user.LockAccount(lockoutEnd);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to lock the user: {errors}");
        }

        return new LockUserResult(user.Id, lockoutEnd);
    }
}
