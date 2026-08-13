namespace ShopIt.Identity.Application.DataSeeding;

/// <summary>
/// Seeds the static roles and an admin user for a tenant (ABP-style tenant data seeder).
/// </summary>
public interface ITenantDataSeeder
{
    /// <summary>
    /// Provisions the tenant's static roles (with their default permission claims) and
    /// an admin user assigned to every role. Idempotent — safe to run repeatedly.
    /// </summary>
    Task SeedTenantAsync(Guid tenantId, string tenantName, CancellationToken cancellationToken = default);
}
