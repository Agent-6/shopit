using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(UserManager<User> userManager) : ICommandHandler<UpdateUserCommand, UpdateUserResult>
{
    private readonly UserManager<User> _userManager = userManager;

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

        if (request.EmailConfirmed.HasValue && request.EmailConfirmed.Value && !user.EmailConfirmed)
        {
            user.ConfirmEmail();
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));

        if (request.Roles is not null)
        {
            var current = (await _userManager.GetRolesAsync(user)).ToList();
            var toAdd = request.Roles.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
            var toRemove = current.Except(request.Roles, StringComparer.OrdinalIgnoreCase).ToList();

            if (toRemove.Count > 0)
            {
                var res = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
            }

            if (toAdd.Count > 0)
            {
                var res = await _userManager.AddToRolesAsync(user, toAdd);
                if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
            }
        }

        if (request.Claims is not null)
        {
            var existing = (await _userManager.GetClaimsAsync(user)).ToList();

            foreach (var claim in existing)
            {
                var res = await _userManager.RemoveClaimAsync(user, claim);
                if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
            }

            foreach (var claim in request.Claims)
            {
                var res = await _userManager.AddClaimAsync(user, new Claim(claim.Type, claim.Value));
                if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
            }
        }

        return new UpdateUserResult(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.FirstName, user.LastName, user.PhoneNumber, user.IsActive, user.LastModifiedAt);
    }
}
