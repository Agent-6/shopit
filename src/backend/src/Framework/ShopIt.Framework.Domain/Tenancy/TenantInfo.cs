namespace ShopIt.Framework.Domain.Tenancy;

/// <summary>
/// Identifies a tenant to act as. <see cref="Id"/> being <see cref="Guid.Empty"/> means host scope.
/// </summary>
/// <param name="Id">The tenant id, or <see cref="Guid.Empty"/> for host scope.</param>
/// <param name="Name">The tenant name, used for display and diagnostics. <c>"Host"</c> at host scope.</param>
public record TenantInfo(Guid Id, string Name);
