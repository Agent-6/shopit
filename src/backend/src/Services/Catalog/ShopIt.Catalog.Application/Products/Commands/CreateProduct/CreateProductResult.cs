using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Application.Products.Commands.CreateProduct;

/// <summary>
/// The product that was created.
/// </summary>
/// <remarks>
/// <see cref="CreatedOn"/> is accurate immediately — the aggregate records it — while the modification stamps
/// and the actor are written by the persistence convention after the handler returns, so they are not part of
/// this response. A client that needs them reads the product (#79), which reports the stored audit data.
/// </remarks>
/// <param name="Id">The new product's identifier.</param>
/// <param name="Name">The stored (trimmed) name.</param>
/// <param name="Sku">The stored (trimmed) SKU.</param>
/// <param name="Description">The stored description, or <c>null</c>.</param>
/// <param name="Price">The stored price, with the currency it was stamped in.</param>
/// <param name="CategoryId">The category it was filed under, or <c>null</c>.</param>
/// <param name="Status">The lifecycle status; always <see cref="ProductStatus.Draft"/> on creation.</param>
/// <param name="TenantId">The owning tenant, taken from the request's tenant context.</param>
/// <param name="CreatedOn">When the aggregate was created.</param>
public record CreateProductResult(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    Money Price,
    Guid? CategoryId,
    ProductStatus Status,
    Guid TenantId,
    DateTimeOffset CreatedOn);
