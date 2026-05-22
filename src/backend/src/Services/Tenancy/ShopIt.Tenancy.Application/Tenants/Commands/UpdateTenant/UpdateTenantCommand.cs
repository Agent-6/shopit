using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;

public record UpdateTenantCommand(Guid Id, string Name) : ICommand<UpdateTenantResult>;
