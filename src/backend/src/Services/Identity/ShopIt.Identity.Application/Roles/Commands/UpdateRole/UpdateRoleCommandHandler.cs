using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Roles;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler(RoleManager<Role> roleManager)
    : ICommandHandler<UpdateRoleCommand, Result<UpdateRoleResult, Error>>
{
    private readonly RoleManager<Role> _roleManager = roleManager;

    public async Task<Result<UpdateRoleResult, Error>> HandleAsync(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null)
        {
            return RoleErrors.NotFound(request.RoleId);
        }

        role.Update(request.Name, request.Description);

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return RoleErrors.Invalid(
                "Role.Update",
                result.Errors.Select(e => e.Description));
        }

        return new UpdateRoleResult(role.Id, role.Name ?? string.Empty, role.Description, DateTime.UtcNow);
    }
}
