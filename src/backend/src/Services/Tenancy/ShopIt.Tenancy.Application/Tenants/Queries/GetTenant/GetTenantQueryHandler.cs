using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Domain.Repositories;
using ShopIt.Tenancy.Domain.Tenants;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;

public class GetTenantQueryHandler(ITenantRepository tenantRepository)
    : IQueryHandler<GetTenantQuery, Result<GetTenantResult, Error>>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<Result<GetTenantResult, Error>> HandleAsync(GetTenantQuery request, CancellationToken cancellationToken)
    {
        // Find, not Get: "not found" is an answer this query can give, not an exception.
        var tenant = await _tenantRepository.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound(request.Id);
        }

        return new GetTenantResult(
            Id: tenant.Id,
            Name: tenant.Name,
            IsActive: tenant.IsActive,
            CreatedOn: tenant.CreatedOn,
            LastModifiedOn: tenant.LastModifiedOn
        );
    }
}
