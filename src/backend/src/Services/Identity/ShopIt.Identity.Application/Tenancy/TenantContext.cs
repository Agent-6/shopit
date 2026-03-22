using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.Tenancy;

public class TenantContext : ITenantContext
{
    private readonly AsyncLocal<TenantInfo> _currentTenant = new();

    public Guid CurrentTenantId => _currentTenant.Value?.Id ?? throw new InvalidOperationException("Tenant resolution failed");
    public TenantInfo CurrentTenant => _currentTenant.Value ?? throw new InvalidOperationException("Tenant resolution failed");
    public bool IsMultitenant => _currentTenant.Value is not null;

    public void SetTenant(TenantInfo tenant) => _currentTenant.Value = tenant;
}
