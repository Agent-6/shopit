using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoles;

public class GetRolesQueryHandler(IRoleRepository roleRepository) : IQueryHandler<GetRolesQuery, GetRolesResult>
{
    private readonly IRoleRepository _roleRepository = roleRepository;

    public async Task<GetRolesResult> HandleAsync(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var (roles, total) = await _roleRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.Filter, cancellationToken);

        var items = roles.Select(r => new GetRolesRoleItem(r.Id, r.Name ?? string.Empty, r.Description, r.CreatedAt));
        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);

        return new GetRolesResult(items, total, request.PageNumber, request.PageSize, totalPages);
    }
}
