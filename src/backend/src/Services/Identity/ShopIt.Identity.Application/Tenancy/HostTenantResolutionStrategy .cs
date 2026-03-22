namespace ShopIt.Identity.Application.Tenancy;

public class HostTenantResolutionStrategy : ITenantResolutionStrategy
{
    // For subdomain-based: tenantName.app.com
    public async Task<Guid> GetCurrentTenantIdAsync()
    {
        return Guid.Empty; // Return a default tenant Id for host-level access
    }
}

public class HeaderTenantResolutionStrategy : ITenantResolutionStrategy
{
    // For Tenant-Id header
    public async Task<Guid> GetCurrentTenantIdAsync()
    {
        return Guid.Empty; // Return a default tenant Id for host-level access
    }
}
