using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Tenancy.Application.Tenants.Commands.CreateTenant;

public record CreateTenantCommand(string Name) : ICommand<CreateTenantResult>;
