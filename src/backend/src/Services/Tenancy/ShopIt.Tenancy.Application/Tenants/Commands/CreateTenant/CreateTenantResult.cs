namespace ShopIt.Tenancy.Application.Tenants.Commands.CreateTenant;

/// <summary>
/// The tenant that was created.
/// </summary>
/// <remarks>
/// Creation does not take a currency, so <see cref="Currency"/> is always
/// <see cref="Domain.Entities.Tenant.DefaultCurrency"/>. It is reported anyway so the create response, the update
/// response and the reads all describe a tenant the same way.
/// </remarks>
/// <param name="Id">The new tenant's identifier.</param>
/// <param name="Name">The stored (trimmed) name.</param>
/// <param name="Currency">The ISO 4217 code its prices will be denominated in.</param>
/// <param name="IsActive">Whether the tenant is active; <c>false</c> until it is activated.</param>
/// <param name="CreatedOn">When the aggregate was created.</param>
public record CreateTenantResult(
    Guid Id,
    string Name,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedOn);
