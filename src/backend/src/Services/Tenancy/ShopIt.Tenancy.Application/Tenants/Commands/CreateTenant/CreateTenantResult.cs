namespace ShopIt.Tenancy.Application.Tenants.Commands.CreateTenant;

public record CreateTenantResult(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedOn);
