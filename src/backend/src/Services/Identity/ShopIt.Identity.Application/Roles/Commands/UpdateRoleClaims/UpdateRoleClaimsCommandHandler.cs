using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRoleClaims;

public class UpdateRoleClaimsCommandHandler(RoleManager<Role> roleManager) : ICommandHandler<UpdateRoleClaimsCommand, UpdateRoleClaimsResult>
{
    private readonly RoleManager<Role> _roleManager = roleManager;

    public async Task<UpdateRoleClaimsResult> HandleAsync(UpdateRoleClaimsCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null) throw new KeyNotFoundException("Role not found");

        var existing = (await _roleManager.GetClaimsAsync(role)).ToList();

        // Replace semantics: remove every existing claim, then apply the requested set.
        foreach (var claim in existing)
        {
            var res = await _roleManager.RemoveClaimAsync(role, claim);
            if (!res.Succeeded)
            {
                var errors = string.Join("; ", res.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update role claims: {errors}");
            }
        }

        foreach (var c in request.Claims)
        {
            var res = await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(c.Type, c.Value));
            if (!res.Succeeded)
            {
                var errors = string.Join("; ", res.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update role claims: {errors}");
            }
        }

        return new UpdateRoleClaimsResult(role.Id, request.Claims.ToList(), DateTime.UtcNow);
    }
}
