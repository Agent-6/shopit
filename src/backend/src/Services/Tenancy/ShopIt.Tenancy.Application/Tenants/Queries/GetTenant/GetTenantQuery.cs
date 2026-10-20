using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;

public record GetTenantQuery(Guid Id) : IQuery<Result<GetTenantResult, Error>>;
