using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<DeleteTenantCommand, DeleteTenantResult>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<DeleteTenantResult> HandleAsync(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenant with ID {request.Id} was not found.");

        tenant.Deactivate();
        await _tenantRepository.RemoveAsync(tenant, cancellationToken);

        return new DeleteTenantResult(tenant.Id, true);
    }
}
