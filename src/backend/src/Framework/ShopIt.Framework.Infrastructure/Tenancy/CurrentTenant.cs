using Microsoft.AspNetCore.Http;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Framework.Infrastructure.Tenancy;

/// <summary>
/// Resolves the acting tenant from the <c>tenant_id</c> and <c>tenant_name</c> claims on the current
/// <see cref="HttpContext"/>, and lets background work override it with <see cref="Change"/>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately throws rather than defaulting when there is no HTTP context or the user is not
/// authenticated. Silently falling back to <see cref="Guid.Empty"/> would turn every background scope
/// into a host-scoped one, which is the failure this model is most exposed to.
/// </para>
/// <para>
/// Registered as scoped, because <see cref="Change"/> mutates per-scope state. See
/// <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </para>
/// </remarks>
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
