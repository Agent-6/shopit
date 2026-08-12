using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
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
        var role = Role.Create(
            Guid.NewGuid(),
            request.Name,
            _currentTenant.Id,
            createdBy: "system",
            request.Description);

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role: {errors}");
        }

        return new CreateRoleResult(role.Id, role.Name ?? string.Empty, role.Description, role.CreatedAt);
    }
}
