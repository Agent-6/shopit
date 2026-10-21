namespace ShopIt.Catalog.Application.Categories.Queries.GetCategory;

/// <summary>
/// A single category.
/// </summary>
/// <param name="Id">The category's identifier.</param>
/// <param name="Name">The stored name.</param>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="CreatedOn">When the category was created.</param>
/// <param name="LastModifiedOn">When it was last modified, or <c>null</c> if it never has been.</param>
public record GetCategoryResult(
    Guid Id,
    string Name,
    Guid TenantId,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
