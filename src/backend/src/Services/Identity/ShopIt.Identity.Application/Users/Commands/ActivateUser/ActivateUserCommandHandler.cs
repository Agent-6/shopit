using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.ActivateUser;

public class ActivateUserCommandHandler(UserManager<User> userManager) : ICommandHandler<ActivateUserCommand, ActivateUserResult>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<ActivateUserResult> HandleAsync(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        user.Activate();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to activate the user: {errors}");
        }

        return new ActivateUserResult(user.Id, user.IsActive);
    }
}
