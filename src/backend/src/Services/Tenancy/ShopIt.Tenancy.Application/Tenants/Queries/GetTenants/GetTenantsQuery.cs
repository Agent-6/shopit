using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

public record GetTenantsQuery(
    int Page,
    int PageSize,
    string? Filter) : IQuery<GetTenantsResult>;
