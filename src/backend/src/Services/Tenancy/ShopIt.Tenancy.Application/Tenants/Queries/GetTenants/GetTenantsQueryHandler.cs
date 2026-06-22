using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler(ITenantRepository tenantRepository)
    : IQueryHandler<GetTenantsQuery, GetTenantsResult>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<GetTenantsResult> HandleAsync(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var (tenants, totalCount, totalPages) = await _tenantRepository.GetPagedAsync(
            request.PageNumber,
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

        return new GetTenantsResult(
            Items: items,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }
}
