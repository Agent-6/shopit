namespace ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;

/// <summary>
/// The tenant after the update.
/// </summary>
/// <remarks>
/// <see cref="Currency"/> is here even though this command never changes it: renaming must not be able to report
/// a tenant without the unit its prices are in, and a client should not have to know which operations return the
/// whole row and which do not.
/// </remarks>
/// <param name="Id">The tenant's identifier.</param>
/// <param name="Name">The stored (trimmed) name.</param>
/// <param name="Currency">The ISO 4217 code its new prices are denominated in; unchanged by a rename.</param>
/// <param name="IsActive">Whether the tenant is active.</param>
/// <param name="CreatedOn">When the tenant was created.</param>
/// <param name="LastModifiedOn">When it was last modified.</param>
public record UpdateTenantResult(
    Guid Id,
    string Name,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
