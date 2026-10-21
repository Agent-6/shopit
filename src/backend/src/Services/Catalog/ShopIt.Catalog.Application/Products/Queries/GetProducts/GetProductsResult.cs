namespace ShopIt.Catalog.Application.Products.Queries.GetProducts;

/// <summary>
/// A page of products plus the counts a client needs to page through them.
/// </summary>
/// <param name="Items">The products on this page.</param>
/// <param name="PageNumber">The page that was returned.</param>
/// <param name="PageSize">The size that was requested.</param>
/// <param name="TotalCount">How many products match the filters in this tenant.</param>
/// <param name="TotalPages">How many pages that is.</param>
/// <remarks>
/// <see cref="TotalCount"/> is the count <em>after</em> filtering and <em>before</em> paging: a client drawing
/// page links needs the size of the whole result, not the size of the page it just received.
/// </remarks>
public record GetProductsResult(
    IEnumerable<GetProductsProductItem> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    long TotalPages);
