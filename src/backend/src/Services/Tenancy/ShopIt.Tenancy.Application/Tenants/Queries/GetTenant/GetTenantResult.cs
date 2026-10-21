namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;

/// <summary>
/// One tenant, as the API reports it.
/// </summary>
/// <remarks>
/// <see cref="Currency"/> is part of the read contract because it is what the catalog resolves through the
/// internal endpoint (ADR-0006): a value that decides what every product of this tenant is priced in is not a
/// detail, and an operator needs to see it on the same screen as the tenant's name.
/// </remarks>
/// <param name="Id">The tenant's identifier.</param>
/// <param name="Name">The display name.</param>
/// <param name="Currency">The ISO 4217 code every new price of this tenant is denominated in.</param>
/// <param name="IsActive">Whether the tenant is active.</param>
/// <param name="CreatedOn">When the tenant was created.</param>
/// <param name="LastModifiedOn">When it was last modified, or <c>null</c> if it never has been.</param>
public record GetTenantResult(
    Guid Id,
    string Name,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
