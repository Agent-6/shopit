using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Commands.ActivateTenant;

public class ActivateTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<ActivateTenantCommand, bool>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<bool> HandleAsync(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException($"Tenant with ID {request.Id} was not found.");
        }

        tenant.Activate();

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return true;
    }
}
