using Microsoft.AspNetCore.Http;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.Tenancy;

public class CurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private Lazy<Guid> _id;
    private Lazy<string?> _name;

    public CurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;

        _id = new Lazy<Guid>(ResolveId);
        _name = new Lazy<string?>(ResolveName);
    }

    public Guid Id => _id.Value;
    public string? Name => _name.Value;

    private Guid ResolveId()
    {
        var context = GetValidatedContext();
        var claim = context.User.FindFirst("tenant_id")?.Value;

        if (Guid.TryParse(claim, out var tenantId))
            return tenantId;

        return Guid.Empty; // Host
    }

    private string? ResolveName()
    {
        var context = GetValidatedContext();
        var claim = context.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(claim))
            return "Host";

        return context.User.FindFirst("tenant_name")?.Value;
    }

    private HttpContext GetValidatedContext()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context == null)
            throw new InvalidOperationException("Tenant context missing. Call Change() for background tasks.");

        if (context.User?.Identity?.IsAuthenticated is not true)
            throw new UnauthorizedAccessException("User not authenticated.");

        return context;
    }

    public IDisposable Change(TenantInfo tenantInfo)
    {
        var oldId = _id;
        var oldName = _name;

        // Overwrite the Lazy wrappers with pre-computed values
        _id = new Lazy<Guid>(() => tenantInfo.Id);
        _name = new Lazy<string?>(() => tenantInfo.Name);

        return new DisposeAction(() =>
        {
            // Restore the original resolution logic when disposed
            _id = oldId;
            _name = oldName;
        });
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
