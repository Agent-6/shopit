namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;

public record GetTenantResult(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
