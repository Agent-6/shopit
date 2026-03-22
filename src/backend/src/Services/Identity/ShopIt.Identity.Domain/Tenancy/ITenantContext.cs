namespace ShopIt.Identity.Domain.Tenancy;

public interface ITenantContext
{
    Guid CurrentTenantId { get; }
    TenantInfo CurrentTenant { get; }
    bool IsMultitenant { get; }
    void SetTenant(TenantInfo tenant);
}
