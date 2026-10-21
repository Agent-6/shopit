using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Application.Products.Queries.GetProduct;

/// <summary>
/// A single product, with everything a detail view or an edit form needs.
/// </summary>
/// <remarks>
/// The price is <see cref="Money"/>, not a <c>decimal</c>: ADR-0005 puts the amount on the wire as a string,
/// and a DTO that flattened it would round a price on its way through JSON. <see cref="CategoryId"/> is
/// <c>null</c> for an uncategorised product — an absent category is a normal state, not a failure.
/// </remarks>
/// <param name="Id">The product's identifier.</param>
/// <param name="Name">The stored name.</param>
/// <param name="Sku">The stored SKU, with its original casing.</param>
/// <param name="Description">The stored description, or <c>null</c> when the product has none.</param>
/// <param name="Price">The stored price, in its stored currency.</param>
/// <param name="CategoryId">The category the product is filed under, or <c>null</c> when uncategorised.</param>
/// <param name="Status">Where the product is in its lifecycle.</param>
/// <param name="TenantId">The owning tenant, which is always the acting tenant.</param>
/// <param name="CreatedOn">When the product was created.</param>
/// <param name="LastModifiedOn">When it was last modified, or <c>null</c> if it never has been.</param>
public record GetProductResult(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    Money Price,
    Guid? CategoryId,
    ProductStatus Status,
    Guid TenantId,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
