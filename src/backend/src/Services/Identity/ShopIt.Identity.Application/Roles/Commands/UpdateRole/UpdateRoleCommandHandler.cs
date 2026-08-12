using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler(RoleManager<Role> roleManager) : ICommandHandler<UpdateRoleCommand, UpdateRoleResult>
{
    private readonly RoleManager<Role> _roleManager = roleManager;

    public async Task<UpdateRoleResult> HandleAsync(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null) throw new KeyNotFoundException("Role not found");

        role.Update(request.Name, request.Description);

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update role: {errors}");
        }

        return new UpdateRoleResult(role.Id, role.Name ?? string.Empty, role.Description, DateTime.UtcNow);
    }
}
