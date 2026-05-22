using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Tenancy.Application.Tenants.Commands.ActivateTenant;

public record ActivateTenantCommand(Guid Id) : ICommand<bool>;
