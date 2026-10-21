using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Categories.Queries.GetCategory;

/// <summary>
/// Reads one category of the acting tenant.
/// </summary>
/// <param name="Id">The category to read.</param>
public record GetCategoryQuery(Guid Id) : IQuery<Result<GetCategoryResult, Error>>;
