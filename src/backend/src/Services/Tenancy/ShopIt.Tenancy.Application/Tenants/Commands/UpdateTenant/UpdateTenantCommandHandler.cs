using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Domain.Repositories;
using ShopIt.Tenancy.Domain.Tenants;

namespace ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<UpdateTenantCommand, Result<UpdateTenantResult, Error>>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<Result<UpdateTenantResult, Error>> HandleAsync(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound(request.Id);
        }

        var normalizedNewName = request.Name.Trim();
        if (!string.Equals(tenant.Name, normalizedNewName, StringComparison.OrdinalIgnoreCase)
            && await _tenantRepository.ExistsByNameAsync(normalizedNewName, cancellationToken))
        {
            return TenantErrors.NameAlreadyExists(request.Name);
        }

        tenant.Update(request.Name);

        // Audit fields are stamped by the persistence path (ADR-0004), not here.

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        return new UpdateTenantResult(
            Id: tenant.Id,
            Name: tenant.Name,
            Currency: tenant.Currency,
            IsActive: tenant.IsActive,
            CreatedOn: tenant.CreatedOn,
            LastModifiedOn: tenant.LastModifiedOn
        );
    }
}
