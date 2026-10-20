using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Repositories;
using ShopIt.Identity.Domain.Roles;

namespace ShopIt.Identity.Application.Roles.Queries.GetRole;

public class GetRoleQueryHandler(IRoleRepository roleRepository)
    : IQueryHandler<GetRoleQuery, Result<GetRoleResult, Error>>
{
    private readonly IRoleRepository _roleRepository = roleRepository;

    public async Task<Result<GetRoleResult, Error>> HandleAsync(GetRoleQuery request, CancellationToken cancellationToken)
    {
        // Find, not Get: a role that does not exist is an answer this query can give (#32).
        var role = await _roleRepository.FindByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return RoleErrors.NotFound(request.RoleId);
        }

        return new GetRoleResult(role.Id, role.Name ?? string.Empty, role.Description, role.CreatedOn.UtcDateTime, role.MultiTenancySide);
    }
}
