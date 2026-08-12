using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoleClaims;

public class GetRoleClaimsQueryHandler(RoleManager<Role> roleManager) : IQueryHandler<GetRoleClaimsQuery, GetRoleClaimsResult>
{
    private readonly RoleManager<Role> _roleManager = roleManager;

    public async Task<GetRoleClaimsResult> HandleAsync(GetRoleClaimsQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null) throw new KeyNotFoundException("Role not found");

        var claims = await _roleManager.GetClaimsAsync(role);
        var items = claims.Select(c => new RoleClaimItem(c.Type, c.Value));

        return new GetRoleClaimsResult(role.Id, items);
    }
}
