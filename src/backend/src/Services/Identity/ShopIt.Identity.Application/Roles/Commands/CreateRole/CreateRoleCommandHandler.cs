using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler(
    RoleManager<Role> roleManager,
    ICurrentTenant currentTenant) : ICommandHandler<CreateRoleCommand, CreateRoleResult>
{
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<CreateRoleResult> HandleAsync(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // A role's declared side must be available on the caller's side: a host caller
        // may create host or both-side roles, a tenant caller tenant or both-side ones.
        // A tenant caller declaring a host-only role (or vice versa) is rejected.
        var currentSide = _currentTenant.IsHost
            ? PermissionMultiTenancySide.Host
            : PermissionMultiTenancySide.Tenant;
        var side = request.MultiTenancySide ?? PermissionMultiTenancySide.Both;

        if (!side.IsAvailableOn(currentSide))
        {
            throw new InvalidOperationException(
                $"Role '{request.Name}' is not available on the {currentSide} side; " +
                $"choose '{PermissionMultiTenancySide.Both}' or '{currentSide}'.");
        }

        var role = Role.Create(
            Guid.NewGuid(),
            request.Name,
            _currentTenant.Id,
            createdBy: "system",
            request.Description,
            side);

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role: {errors}");
        }

        return new CreateRoleResult(role.Id, role.Name ?? string.Empty, role.Description, role.CreatedAt, role.MultiTenancySide);
    }
}
