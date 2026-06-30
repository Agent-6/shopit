namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

public record GetTenantsResult(
    IEnumerable<GetTenantsTenantItem> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    long TotalPages);
