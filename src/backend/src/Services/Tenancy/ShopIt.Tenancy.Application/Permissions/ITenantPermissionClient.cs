namespace ShopIt.Tenancy.Application.Permissions;

/// <summary>
/// Resolves the effective permission set for a user. Permissions are owned by the Identity
/// service (user/role claims), so implementations typically delegate to Identity over HTTP.
/// </summary>
public interface ITenantPermissionClient
{
    Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
