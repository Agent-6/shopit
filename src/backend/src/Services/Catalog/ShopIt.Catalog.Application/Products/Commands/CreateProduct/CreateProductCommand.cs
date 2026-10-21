using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Products.Commands.CreateProduct;

/// <summary>
/// Creates a product in the acting tenant, in <see cref="ProductStatus.Draft"/>.
/// </summary>
/// <remarks>
/// The price arrives as an amount plus a currency rather than as a <see cref="Money"/> because a request with
/// a negative or over-precise amount must fail as a field-level <c>Result</c>, not as a deserialization
/// exception — constructing the value object is what validates it, and that happens in the handler.
/// </remarks>
/// <param name="Name">The display name; trimmed and validated by the handler.</param>
/// <param name="Sku">The stock-keeping unit, unique within the tenant once trimmed.</param>
/// <param name="Description">An optional description.</param>
/// <param name="Amount">The price amount; non-negative, at most two decimal places.</param>
/// <param name="Currency">The ISO 4217 code, or <c>null</c> to accept the catalog's configured currency.</param>
/// <param name="CategoryId">An optional category, which must exist in the acting tenant.</param>
public record CreateProductCommand(
    string? Name,
    string? Sku,
    string? Description,
    decimal Amount,
    string? Currency,
    Guid? CategoryId) : ICommand<Result<CreateProductResult, Error>>;
