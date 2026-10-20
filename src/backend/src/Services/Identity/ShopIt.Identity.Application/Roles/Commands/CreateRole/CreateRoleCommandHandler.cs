using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Roles;

namespace ShopIt.Identity.Application.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler(
    RoleManager<Role> roleManager,
    ICurrentTenant currentTenant) : ICommandHandler<CreateRoleCommand, Result<CreateRoleResult, Error>>
{
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<CreateRoleResult, Error>> HandleAsync(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // A role's declared side must be available on the caller's side: a host caller
        // may create host or both-side roles, a tenant caller tenant or both-side ones.
        var currentSide = _currentTenant.IsHost
            ? PermissionMultiTenancySide.Host
            : PermissionMultiTenancySide.Tenant;
        var side = request.MultiTenancySide ?? PermissionMultiTenancySide.Both;

        if (!side.IsAvailableOn(currentSide))
        {
            return RoleErrors.NotAvailableOnSide(request.Name, currentSide);
        }

        var role = Role.Create(
            Guid.NewGuid(),
            request.Name,
            _currentTenant.Id,
            request.Description,
            side);

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return RoleErrors.Invalid(
                "Role.Create",
                result.Errors.Select(e => e.Description));
        }

        return new CreateRoleResult(role.Id, role.Name ?? string.Empty, role.Description, role.CreatedOn.UtcDateTime, role.MultiTenancySide);
    }
}
