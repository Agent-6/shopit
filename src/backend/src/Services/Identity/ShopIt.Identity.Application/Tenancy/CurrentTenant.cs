using Microsoft.AspNetCore.Http;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.Tenancy;

public class CurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private Guid? _id;
    private string? _name;

    public Guid Id => _id ?? GetId();
    public string? Name => _name;

    public IDisposable Change(TenantInfo tenant)
    {
        // Save original state
        var originalId = _id;
        var originalName = _name;

        // Apply new tenant
        _id = tenant.Id;
        _name = tenant.Name;

        // Return disposable that restores original state
        return new DisposeAction(() =>
        {
            _id = originalId;
            _name = originalName;
        });
    }

    public Guid GetId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated is not true)
        {
            throw new InvalidOperationException("User is not authenticated. Cannot resolve tenant.");
        }

        var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
        {
            _id = tenantId;

            // Optionally, you could also extract tenant name from claims if available
            _name = httpContext.User.FindFirst("tenant_name")?.Value;
        }

        _id = Guid.Empty; // Host-level access
        _name = "Host";
        return _id.Value;
    }
}

// Simple disposable helper
public sealed class DisposeAction(Action action) : IDisposable
{
    private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));
    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            _action();
            _disposed = true;
        }
    }
}
