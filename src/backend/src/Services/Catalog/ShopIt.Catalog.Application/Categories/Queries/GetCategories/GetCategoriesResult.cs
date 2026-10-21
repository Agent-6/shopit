namespace ShopIt.Catalog.Application.Categories.Queries.GetCategories;

/// <summary>
/// A page of categories plus the counts a client needs to page through them.
/// </summary>
/// <param name="Items">The categories on this page.</param>
/// <param name="PageNumber">The page that was returned.</param>
/// <param name="PageSize">The size that was requested.</param>
/// <param name="TotalCount">How many categories match the filter in this tenant.</param>
/// <param name="TotalPages">How many pages that is.</param>
public record GetCategoriesResult(
    IEnumerable<GetCategoriesCategoryItem> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    long TotalPages);
