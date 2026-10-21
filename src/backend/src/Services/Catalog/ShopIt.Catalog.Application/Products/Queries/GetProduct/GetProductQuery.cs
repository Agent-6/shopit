using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Products.Queries.GetProduct;

/// <summary>
/// Reads one product of the acting tenant.
/// </summary>
/// <param name="Id">The product to read.</param>
public record GetProductQuery(Guid Id) : IQuery<Result<GetProductResult, Error>>;
