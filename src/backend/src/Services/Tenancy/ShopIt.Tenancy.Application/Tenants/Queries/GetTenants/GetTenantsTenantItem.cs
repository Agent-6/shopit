namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

/// <summary>
/// One tenant in a page of results.
/// </summary>
/// <param name="Id">The tenant's identifier.</param>
/// <param name="Name">The display name.</param>
/// <param name="Currency">The ISO 4217 code every new price of this tenant is denominated in.</param>
/// <param name="IsActive">Whether the tenant is active.</param>
/// <param name="CreatedOn">When the tenant was created.</param>
/// <param name="LastModifiedOn">When it was last modified, or <c>null</c> if it never has been.</param>
public record GetTenantsTenantItem(
    Guid Id,
    string Name,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
