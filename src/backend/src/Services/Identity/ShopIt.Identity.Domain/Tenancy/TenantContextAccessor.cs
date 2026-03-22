namespace ShopIt.Identity.Domain.Tenancy;

public static class TenantContextAccessor
{
    private static readonly AsyncLocal<ITenantContext> _current = new();

    public static ITenantContext Current
    {
        get => _current.Value!;
        set => _current.Value = value;
    }
}
