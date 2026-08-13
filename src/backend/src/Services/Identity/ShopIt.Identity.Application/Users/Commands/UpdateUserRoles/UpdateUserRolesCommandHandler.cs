using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserRoles;

public class UpdateUserRolesCommandHandler(UserManager<User> userManager) : ICommandHandler<UpdateUserRolesCommand, UpdateUserRolesResult>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<UpdateUserRolesResult> HandleAsync(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var requested = request.RoleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var current = (await _userManager.GetRolesAsync(user)).ToList();

        var toAdd = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
        var toRemove = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0)
        {
            var res = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!res.Succeeded)
            {
                var errors = string.Join("; ", res.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update user roles: {errors}");
            }
        }

        if (toAdd.Count > 0)
        {
            var res = await _userManager.AddToRolesAsync(user, toAdd);
            if (!res.Succeeded)
            {
                var errors = string.Join("; ", res.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update user roles: {errors}");
            }
        }

        return new UpdateUserRolesResult(user.Id, requested, DateTime.UtcNow);
    }
}
