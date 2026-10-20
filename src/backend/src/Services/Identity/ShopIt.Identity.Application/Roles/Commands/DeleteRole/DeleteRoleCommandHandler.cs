using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;
using ShopIt.Identity.Domain.Roles;

namespace ShopIt.Identity.Application.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler(
    RoleManager<Role> roleManager,
    IRoleRepository roleRepository) : ICommandHandler<DeleteRoleCommand, Result<DeleteRoleResult, Error>>
{
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly IRoleRepository _roleRepository = roleRepository;

    public async Task<Result<DeleteRoleResult, Error>> HandleAsync(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null)
        {
            return RoleErrors.NotFound(request.RoleId);
        }

        var assignedUsers = await _roleRepository.CountUsersInRoleAsync(role.Id, cancellationToken);
        if (assignedUsers > 0)
        {
            return RoleErrors.InUse(role.Name, assignedUsers);
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return RoleErrors.Invalid(
                "Role.Delete",
                result.Errors.Select(e => e.Description));
        }

        return new DeleteRoleResult(role.Id, true);
    }
}
