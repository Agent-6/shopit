namespace ShopIt.Catalog.Application.Categories.Commands.CreateCategory;

/// <summary>
/// The category that was created.
/// </summary>
/// <param name="Id">The new category's identifier.</param>
/// <param name="Name">The stored (trimmed) name.</param>
/// <param name="TenantId">The owning tenant, taken from the request's tenant context.</param>
/// <param name="CreatedOn">When the aggregate was created.</param>
public record CreateCategoryResult(
    Guid Id,
    string Name,
    Guid TenantId,
    DateTimeOffset CreatedOn);
