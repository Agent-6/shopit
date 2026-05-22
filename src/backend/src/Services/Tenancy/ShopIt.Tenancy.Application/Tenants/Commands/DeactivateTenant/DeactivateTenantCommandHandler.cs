using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeactivateTenant;

public class DeactivateTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<DeactivateTenantCommand, bool>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<bool> HandleAsync(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException($"Tenant with ID {request.Id} was not found.");
        }

        tenant.Deactivate();
        
        tenant.LastModifiedOn = DateTimeOffset.UtcNow;
        tenant.LastModifiedBy = Guid.Empty; // System/Host Level

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return true;
    }
}
