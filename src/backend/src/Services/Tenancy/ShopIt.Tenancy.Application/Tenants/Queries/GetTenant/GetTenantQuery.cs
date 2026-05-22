using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;

public record GetTenantQuery(Guid Id) : IQuery<GetTenantResult>;
