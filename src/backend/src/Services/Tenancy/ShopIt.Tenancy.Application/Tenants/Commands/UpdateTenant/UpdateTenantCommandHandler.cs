using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<UpdateTenantCommand, UpdateTenantResult>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<UpdateTenantResult> HandleAsync(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException($"Tenant with ID {request.Id} was not found.");
        }

        var normalizedNewName = request.Name.Trim();
        if (!string.Equals(tenant.Name, normalizedNewName, StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _tenantRepository.ExistsByNameAsync(normalizedNewName, cancellationToken);
            if (nameExists)
            {
                throw new InvalidOperationException($"A tenant with the name '{request.Name}' already exists.");
            }
        }

        tenant.Update(request.Name);
        
        tenant.LastModifiedOn = DateTimeOffset.UtcNow;
        tenant.LastModifiedBy = Guid.Empty; // System/Host Level

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return new UpdateTenantResult(
            Id: tenant.Id,
            Name: tenant.Name,
            IsActive: tenant.IsActive,
            CreatedOn: tenant.CreatedOn,
            LastModifiedOn: tenant.LastModifiedOn
        );
    }
}
