using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Application.Products.Commands.UpdateProduct;

/// <summary>
/// The product after the update.
/// </summary>
/// <remarks>
/// <see cref="LastModifiedOn"/> and the actor are stamped by the persistence convention, which runs after the
/// handler returns, so this response still reports the values from before the change. They are kept so the
/// shape matches the create response and a client does not have to care.
/// </remarks>
/// <param name="Id">The product's identifier.</param>
/// <param name="Name">The stored (trimmed) name.</param>
/// <param name="Sku">The stored (trimmed) SKU.</param>
/// <param name="Description">The stored description, or <c>null</c>.</param>
/// <param name="Price">The stored price.</param>
/// <param name="CategoryId">The category it is filed under, or <c>null</c>.</param>
/// <param name="Status">The lifecycle status; unchanged by an update.</param>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="CreatedOn">When the product was created; unchanged by an update.</param>
/// <param name="LastModifiedOn">When it was last modified.</param>
public record UpdateProductResult(
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
