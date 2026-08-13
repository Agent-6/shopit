using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.UnlockUser;

public class UnlockUserCommandHandler(UserManager<User> userManager) : ICommandHandler<UnlockUserCommand, UnlockUserResult>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<UnlockUserResult> HandleAsync(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        user.UnlockAccount();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to unlock the user: {errors}");
        }

        return new UnlockUserResult(user.Id, true);
    }
}
