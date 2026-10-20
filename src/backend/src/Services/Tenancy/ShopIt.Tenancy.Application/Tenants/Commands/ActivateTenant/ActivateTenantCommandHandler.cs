using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Domain.Repositories;
using ShopIt.Tenancy.Domain.Tenants;

namespace ShopIt.Tenancy.Application.Tenants.Commands.ActivateTenant;

public class ActivateTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<ActivateTenantCommand, Result<bool, Error>>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<Result<bool, Error>> HandleAsync(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound(request.Id);
        }

        tenant.Activate();

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return true;
    }
}
