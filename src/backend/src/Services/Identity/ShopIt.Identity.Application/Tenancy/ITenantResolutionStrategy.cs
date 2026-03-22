namespace ShopIt.Identity.Application.Tenancy;

public interface ITenantResolutionStrategy
{
    Task<Guid> GetCurrentTenantIdAsync();
}
