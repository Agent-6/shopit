using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler(ITenantRepository tenantRepository)
    : IQueryHandler<GetTenantsQuery, GetTenantsResult>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<GetTenantsResult> HandleAsync(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var (tenants, totalCount) = await _tenantRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Filter,
            cancellationToken
        );

        var items = tenants.Select(t => new GetTenantsTenantItem(
            Id: t.Id,
            Name: t.Name,
            IsActive: t.IsActive,
            CreatedOn: t.CreatedOn,
            LastModifiedOn: t.LastModifiedOn
        ));

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new GetTenantsResult(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages
        );
    }
}
