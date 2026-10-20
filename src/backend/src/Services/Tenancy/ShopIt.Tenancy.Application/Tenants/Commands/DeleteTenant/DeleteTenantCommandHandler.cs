using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Domain.Repositories;
using ShopIt.Tenancy.Domain.Tenants;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<DeleteTenantCommand, Result<DeleteTenantResult, Error>>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<Result<DeleteTenantResult, Error>> HandleAsync(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound(request.Id);
        }

        tenant.Deactivate();
        await _tenantRepository.RemoveAsync(tenant, cancellationToken);

        return new DeleteTenantResult(tenant.Id, true);
    }
}
