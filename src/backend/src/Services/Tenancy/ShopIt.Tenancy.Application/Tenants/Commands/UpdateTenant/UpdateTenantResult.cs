namespace ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;

public record UpdateTenantResult(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
