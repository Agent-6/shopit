using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Roles.Queries.GetRole;

public class GetRoleQueryHandler(IRoleRepository roleRepository) : IQueryHandler<GetRoleQuery, GetRoleResult>
{
    private readonly IRoleRepository _roleRepository = roleRepository;

    public async Task<GetRoleResult> HandleAsync(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

        return new GetRoleResult(role.Id, role.Name ?? string.Empty, role.Description, role.CreatedAt, role.MultiTenancySide);
    }
}
