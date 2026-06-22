namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

public record GetTenantsTenantItem(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
