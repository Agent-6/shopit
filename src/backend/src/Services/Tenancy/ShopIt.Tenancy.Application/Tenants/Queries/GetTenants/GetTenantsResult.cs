namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

public record GetTenantsResult(
    IEnumerable<GetTenantsTenantItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
