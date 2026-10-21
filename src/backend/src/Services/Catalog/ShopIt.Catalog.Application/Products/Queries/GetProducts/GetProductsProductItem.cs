using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Application.Products.Queries.GetProducts;

/// <summary>
/// One product in a page of results.
/// </summary>
/// <remarks>
/// The list deliberately carries the price as <see cref="Money"/> rather than a <c>decimal</c>: ADR-0005's
/// contract is that the amount reaches the client as a string, and a DTO that flattened it to a decimal would
/// round it on the way through JSON. <see cref="TenantId"/> is not reported — the caller already knows which
/// tenant it is acting as, and a list is not the place to confirm it.
/// <para>
/// Unlike the detail read, a page does not report the tenant's <em>current</em> currency. A list is the browse path:
/// it should not depend on another service answering, and a reader who needs to know that a shop has switched (so that
/// a stored <c>EUR</c> price is not mistaken for the <c>GBP</c> the shop now quotes) reads the product (#84, ADR-0006).
/// </para>
/// </remarks>
/// <param name="Id">The product's identifier.</param>
/// <param name="Name">The stored name.</param>
/// <param name="Sku">The stored SKU, with its original casing.</param>
/// <param name="Description">The stored description, or <c>null</c> when the product has none.</param>
/// <param name="Price">The stored price, in its stored currency.</param>
/// <param name="CategoryId">The category the product is filed under, or <c>null</c> when uncategorised.</param>
/// <param name="Status">Where the product is in its lifecycle.</param>
/// <param name="CreatedOn">When the product was created.</param>
/// <param name="LastModifiedOn">When it was last modified, or <c>null</c> if it never has been.</param>
public record GetProductsProductItem(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    Money Price,
    Guid? CategoryId,
    ProductStatus Status,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
