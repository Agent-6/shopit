namespace ShopIt.Catalog.Application.Categories.Queries.GetCategories;

/// <summary>
/// One category in a page of results.
/// </summary>
/// <param name="Id">The category's identifier.</param>
/// <param name="Name">The stored name.</param>
/// <param name="CreatedOn">When the category was created.</param>
/// <param name="LastModifiedOn">When it was last modified, or <c>null</c> if it never has been.</param>
public record GetCategoriesCategoryItem(
    Guid Id,
    string Name,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
