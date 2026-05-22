using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeactivateTenant;

public record DeactivateTenantCommand(Guid Id) : ICommand<bool>;
