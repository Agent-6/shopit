using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Roles;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoleClaims;

public class GetRoleClaimsQueryHandler(RoleManager<Role> roleManager)
    : IQueryHandler<GetRoleClaimsQuery, Result<GetRoleClaimsResult, Error>>
{
    private readonly RoleManager<Role> _roleManager = roleManager;

    public async Task<Result<GetRoleClaimsResult, Error>> HandleAsync(GetRoleClaimsQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null)
        {
            return RoleErrors.NotFound(request.RoleId);
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        var items = claims.Select(c => new RoleClaimItem(c.Type, c.Value));

        return new GetRoleClaimsResult(role.Id, items);
    }
}
