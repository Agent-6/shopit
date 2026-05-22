using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeleteTenant;

public record DeleteTenantCommand(Guid Id) : ICommand<DeleteTenantResult>;
