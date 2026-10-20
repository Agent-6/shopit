using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserRoles;

public class UpdateUserRolesCommandHandler(
    UserManager<User> userManager,
    IRoleRepository roleRepository,
    IPermissionCacheInvalidator permissionCache) : ICommandHandler<UpdateUserRolesCommand, Result<UpdateUserRolesResult, Error>>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IPermissionCacheInvalidator _permissionCache = permissionCache;

    public async Task<Result<UpdateUserRolesResult, Error>> HandleAsync(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(request.UserId);
        }

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
                return UserErrors.RoleNotAvailableOnSide(roleName, role.MultiTenancySide, userSide);
            }
        }

        var toAdd = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
        var toRemove = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0)
        {
            var res = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!res.Succeeded)
            {
            return UserErrors.Invalid("User.Roles", res.Errors.Select(e => e.Description));
            }
        }

        if (toAdd.Count > 0)
        {
            var res = await _userManager.AddToRolesAsync(user, toAdd);
            if (!res.Succeeded)
            {
            return UserErrors.Invalid("User.Roles", res.Errors.Select(e => e.Description));
            }
        }

        // A user's roles decide their permissions, so any change to the assignment invalidates them.
        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            await _permissionCache.InvalidateUserAsync(user.Id, cancellationToken);
        }

        return new UpdateUserRolesResult(user.Id, requested, DateTime.UtcNow);
    }
}
