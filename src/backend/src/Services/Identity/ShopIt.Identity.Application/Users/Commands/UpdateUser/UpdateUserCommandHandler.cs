using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UpdateUserResult>
{
    private readonly UserManager<User> _userManager;

    public UpdateUserCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UpdateUserResult> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrWhiteSpace(request.Email) && user.Email != request.Email)
            user.ChangeEmail(request.Email);

        if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
            user.UpdateProfile(request.FirstName ?? string.Empty, request.LastName ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.SetPhoneNumber(request.PhoneNumber);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) user.Activate(); else user.Deactivate("updated");
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));

        return new UpdateUserResult(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.FirstName, user.LastName, user.PhoneNumber, user.IsActive, DateTime.UtcNow);
    }
}
