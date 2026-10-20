using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeleteTenant;

public record DeleteTenantCommand(Guid Id) : ICommand<Result<DeleteTenantResult, Error>>;
