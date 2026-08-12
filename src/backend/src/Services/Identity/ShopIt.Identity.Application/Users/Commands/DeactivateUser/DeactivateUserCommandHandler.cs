using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.DeactivateUser;

public class DeactivateUserCommandHandler(UserManager<User> userManager) : ICommandHandler<DeactivateUserCommand, DeactivateUserResult>
{
    private readonly UserManager<User> _userManager = userManager;

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

        return new DeactivateUserResult(user.Id, user.IsActive);
    }
}
