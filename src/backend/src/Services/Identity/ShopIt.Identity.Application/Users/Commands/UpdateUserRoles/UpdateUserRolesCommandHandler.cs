using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserRoles;

public class UpdateUserRolesCommandHandler(
    UserManager<User> userManager,
    IRoleRepository roleRepository) : ICommandHandler<UpdateUserRolesCommand, UpdateUserRolesResult>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IRoleRepository _roleRepository = roleRepository;

    public async Task<UpdateUserRolesResult> HandleAsync(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var requested = request.RoleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var current = (await _userManager.GetRolesAsync(user)).ToList();

        // Roles are only assignable on the side they are available on: a host-only
        // role must never be assigned to a tenant user and vice versa. Resolve each
        // requested role within the caller's tenant (where it would actually be
        // looked up for the assignment) and check its declared side.
        var userSide = user.TenantId == Guid.Empty
            ? PermissionMultiTenancySide.Host
            : PermissionMultiTenancySide.Tenant;

        foreach (var roleName in requested)
        {
            var role = await _roleRepository.FindByNameAsync(roleName, cancellationToken);
            if (role is null)
            {
                continue; // role resolution for the assignment itself reports the failure
            }

            if (!role.MultiTenancySide.IsAvailableOn(userSide))
            {
                throw new InvalidOperationException(
                    $"Role '{roleName}' is only available on the {role.MultiTenancySide} side " +
                    $"and cannot be assigned to a user in the {userSide} side.");
            }
        }

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
