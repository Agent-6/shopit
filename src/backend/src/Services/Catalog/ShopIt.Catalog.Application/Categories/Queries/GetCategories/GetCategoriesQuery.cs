using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Categories.Queries.GetCategories;

/// <summary>
/// Lists the acting tenant's categories, one page at a time.
/// </summary>
/// <param name="PageNumber">The 1-based page number.</param>
/// <param name="PageSize">The page size; between 1 and <see cref="MaxPageSize"/>.</param>
/// <param name="Filter">An optional case-insensitive substring to match against the name.</param>
public record GetCategoriesQuery(
    int PageNumber,
    int PageSize,
    string? Filter) : IQuery<Result<GetCategoriesResult, Error>>
{
    /// <summary>The largest page this endpoint will return.</summary>
    /// <remarks>
    /// A cap rather than just a validation rule: an uncapped page size lets one client ask for the whole
    /// catalog, which is the query that takes the database down when a tenant grows.
    /// </remarks>
    public const int MaxPageSize = 100;
}
