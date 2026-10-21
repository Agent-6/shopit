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

        // Audit fields are stamped from the acting user by the persistence path (see ADR-0004); the
        // handler no longer guesses, and no longer writes a placeholder id.
        await _tenantRepository.AddAsync(tenant, cancellationToken);

        return new CreateTenantResult(
            Id: tenant.Id,
            Name: tenant.Name,
            Currency: tenant.Currency,
            IsActive: tenant.IsActive,
            CreatedOn: tenant.CreatedOn
        );
    }
}
