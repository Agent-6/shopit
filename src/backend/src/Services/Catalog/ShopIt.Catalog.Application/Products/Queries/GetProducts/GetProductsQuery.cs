using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Products.Queries.GetProducts;

/// <summary>
/// Lists the acting tenant's products, one page at a time, with optional filtering and ordering.
/// </summary>
/// <param name="PageNumber">The 1-based page number.</param>
/// <param name="PageSize">The page size; between 1 and <see cref="MaxPageSize"/>.</param>
/// <param name="Filter">An optional case-insensitive term matched against the name or the SKU.</param>
/// <param name="CategoryId">An optional category to filter by.</param>
/// <param name="Status">An optional lifecycle status to filter by.</param>
/// <param name="SortBy">The column to order by, one of <see cref="SupportedSortBy"/>.</param>
/// <param name="SortOrder">The direction, one of <see cref="SupportedSortOrder"/>.</param>
/// <remarks>
/// <para>
/// The sort values stay strings here and are resolved by the handler, because the HTTP query string is text:
/// turning them into <see cref="ProductSortField"/> / <see cref="ProductSortOrder"/> is the first decision the
/// use case makes, and an unrecognised value must fail as a <c>Result</c> rather than fall back to a default.
/// </para>
/// <para>
/// <see cref="SortFields"/> is the <em>only</em> list of accepted keys — the parser reads it and the validation
/// message quotes it — so a key cannot be accepted without being documented, or vice versa. An absent or
/// whitespace value means the default; anything else that is not in the dictionary is a 400.
/// </para>
/// </remarks>
public record GetProductsQuery(
    int PageNumber,
    int PageSize,
    string? Filter,
    Guid? CategoryId,
    ProductStatus? Status,
    string? SortBy,
    string? SortOrder) : IQuery<Result<GetProductsResult, Error>>
{
    /// <summary>The largest page this endpoint will return.</summary>
    /// <remarks>
    /// A cap rather than just a validation rule: an uncapped page size lets one client ask for the whole
    /// catalog, which is the query that takes the database down when a tenant grows.
    /// </remarks>
    public const int MaxPageSize = 100;

    /// <summary>The order used when the client does not ask for one.</summary>
    public const string DefaultSortBy = "name";

    /// <summary>The direction used when the client does not ask for one.</summary>
    public const string DefaultSortOrder = "asc";

    /// <summary>The accepted sort keys and the field each one orders by; matched case-insensitively.</summary>
    public static readonly IReadOnlyDictionary<string, ProductSortField> SortFields =
        new Dictionary<string, ProductSortField>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = ProductSortField.Name,
            ["sku"] = ProductSortField.Sku,
            ["price"] = ProductSortField.Price,
            ["createdOn"] = ProductSortField.CreatedOn
        };

    /// <summary>The accepted sort directions; matched case-insensitively.</summary>
    public static readonly IReadOnlyDictionary<string, ProductSortOrder> SortOrders =
        new Dictionary<string, ProductSortOrder>(StringComparer.OrdinalIgnoreCase)
        {
            ["asc"] = ProductSortOrder.Ascending,
            ["desc"] = ProductSortOrder.Descending
        };

    /// <summary>The sort keys this endpoint accepts, for the validation message.</summary>
    public static IEnumerable<string> SupportedSortBy => SortFields.Keys;

    /// <summary>The sort directions this endpoint accepts, for the validation message.</summary>
    public static IEnumerable<string> SupportedSortOrder => SortOrders.Keys;
}
