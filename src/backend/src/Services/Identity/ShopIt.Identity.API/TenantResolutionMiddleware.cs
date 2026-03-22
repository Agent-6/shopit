using ShopIt.Identity.Application.Tenancy;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.API;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context,
        ITenantResolutionStrategy resolutionStrategy,
        ITenantContext tenantContext)
    {
        var tenantId = await resolutionStrategy.GetCurrentTenantIdAsync();

        // TODO: Implement tenant retrieval logic based on tenantId. For now, we will set a default tenant.
        //var tenant = await tenantStore.GetTenantAsync(tenantId);
        tenantContext.SetTenant(new(Id:Guid.Empty, Name:"Host"));
        TenantContextAccessor.Current = tenantContext;

        await _next(context);
    }
}
