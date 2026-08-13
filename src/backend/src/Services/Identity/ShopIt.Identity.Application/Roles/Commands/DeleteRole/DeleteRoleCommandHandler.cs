using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler(
    RoleManager<Role> roleManager,
    IRoleRepository roleRepository) : ICommandHandler<DeleteRoleCommand, DeleteRoleResult>
{
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly IRoleRepository _roleRepository = roleRepository;

    public async Task<DeleteRoleResult> HandleAsync(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null) throw new KeyNotFoundException("Role not found");

        var assignedUsers = await _roleRepository.CountUsersInRoleAsync(role.Id, cancellationToken);
        if (assignedUsers > 0)
        {
            throw new InvalidOperationException(
                $"Role '{role.Name}' is still assigned to {assignedUsers} user(s) and cannot be deleted. Unassign the role first.");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete role: {errors}");
        }

        return new DeleteRoleResult(role.Id, true);
    }
}
