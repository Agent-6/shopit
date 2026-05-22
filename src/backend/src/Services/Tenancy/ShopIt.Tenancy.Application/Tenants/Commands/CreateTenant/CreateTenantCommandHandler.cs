using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Tenancy.Domain.Entities;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<CreateTenantCommand, CreateTenantResult>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<CreateTenantResult> HandleAsync(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = Tenant.Create(Guid.NewGuid(), request.Name);
        
        tenant.CreatedOn = DateTimeOffset.UtcNow;
        tenant.CreatedBy = Guid.Empty; // Registered under system-wide scope

        await _tenantRepository.AddAsync(tenant, cancellationToken);

        return new CreateTenantResult(
            Id: tenant.Id,
            Name: tenant.Name,
            IsActive: tenant.IsActive,
            CreatedOn: tenant.CreatedOn
        );
    }
}
