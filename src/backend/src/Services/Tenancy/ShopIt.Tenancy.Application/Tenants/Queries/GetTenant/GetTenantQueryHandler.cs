using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;

public class GetTenantQueryHandler(ITenantRepository tenantRepository)
    : IQueryHandler<GetTenantQuery, GetTenantResult>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<GetTenantResult> HandleAsync(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException($"Tenant with ID {request.Id} was not found.");
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
