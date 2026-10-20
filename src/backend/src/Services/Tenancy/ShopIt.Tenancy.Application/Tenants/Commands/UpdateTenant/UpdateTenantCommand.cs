using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;

public record UpdateTenantCommand(Guid Id, string Name) : ICommand<Result<UpdateTenantResult, Error>>;
