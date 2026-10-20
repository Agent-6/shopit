using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Domain.Repositories;
using ShopIt.Tenancy.Domain.Tenants;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeactivateTenant;

public class DeactivateTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<DeactivateTenantCommand, Result<bool, Error>>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<Result<bool, Error>> HandleAsync(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound(request.Id);
        }

        tenant.Deactivate();

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return true;
    }
}
