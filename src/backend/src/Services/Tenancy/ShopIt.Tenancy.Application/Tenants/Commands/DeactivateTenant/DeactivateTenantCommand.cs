using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeactivateTenant;

public record DeactivateTenantCommand(Guid Id) : ICommand<Result<bool, Error>>;
