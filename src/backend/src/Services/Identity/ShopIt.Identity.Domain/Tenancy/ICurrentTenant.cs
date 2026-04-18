namespace ShopIt.Identity.Domain.Tenancy;

public interface ICurrentTenant
{
    Guid Id { get; }
    string? Name { get; }
    bool IsHost => Id == Guid.Empty;
    IDisposable Change(TenantInfo tenant);
}
