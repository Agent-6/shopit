using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Replaces a product's editable details in the acting tenant.
/// </summary>
/// <remarks>
/// The status is deliberately absent: it moves through <see cref="ChangeProductStatus.ChangeProductStatusCommand"/>
/// only, so an edit can never publish or withdraw a product as a side effect.
/// </remarks>
/// <param name="Id">The product to update.</param>
/// <param name="Name">The new display name; trimmed and validated by the handler.</param>
/// <param name="Sku">The new SKU, unique within the tenant once trimmed.</param>
/// <param name="Description">The new description, or <c>null</c> to remove it.</param>
/// <param name="Amount">The new price amount; non-negative, at most two decimal places.</param>
/// <param name="Currency">The ISO 4217 code, or <c>null</c> to accept the catalog's configured currency.</param>
/// <param name="CategoryId">The new category, which must exist in the acting tenant, or <c>null</c> for none.</param>
public record UpdateProductCommand(
    Guid Id,
    string? Name,
    string? Sku,
    string? Description,
    decimal Amount,
    string? Currency,
    Guid? CategoryId) : ICommand<Result<UpdateProductResult, Error>>;
