using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Application.Products.Queries.GetProduct;

/// <summary>
/// A single product, with everything a detail view or an edit form needs.
/// </summary>
/// <remarks>
/// <para>
/// The price is <see cref="Money"/>, not a <c>decimal</c>: ADR-0005 puts the amount on the wire as a string,
/// and a DTO that flattened it would round a price on its way through JSON. <see cref="CategoryId"/> is
/// <c>null</c> for an uncategorised product — an absent category is a normal state, not a failure.
/// </para>
/// <para>
/// <see cref="TenantCurrency"/> exists so the response reports <em>both</em> units a reader can care about: the
/// currency the stored price is actually in, and the currency the tenant prices in now. They differ after a tenant
/// changes currency, because existing prices are historical facts and are never re-labelled (ADR-0006) — which makes
/// this the field that lets a client say "priced in EUR; this shop now prices in GBP" instead of silently showing a
/// number in the wrong money. It is advisory and <c>null</c> when the tenant registry could not be reached: a read
/// must still work then, because it stamps nothing, and <see cref="Price"/> remains the stored truth.
/// </para>
/// </remarks>
/// <param name="Id">The product's identifier.</param>
/// <param name="Name">The stored name.</param>
/// <param name="Sku">The stored SKU, with its original casing.</param>
/// <param name="Description">The stored description, or <c>null</c> when the product has none.</param>
/// <param name="Price">The stored price, in its stored currency.</param>
/// <param name="CategoryId">The category the product is filed under, or <c>null</c> when uncategorised.</param>
/// <param name="Status">Where the product is in its lifecycle.</param>
/// <param name="TenantId">The owning tenant, which is always the acting tenant.</param>
/// <param name="TenantCurrency">
/// The currency the tenant prices in now, or <c>null</c> when the tenant registry could not be reached.
/// </param>
/// <param name="CreatedOn">When the product was created.</param>
/// <param name="LastModifiedOn">When it was last modified, or <c>null</c> if it never has been.</param>
/// <param name="CreatedBy">The user who created the product, or <c>null</c> when no interactive user did (seeding).</param>
/// <param name="LastModifiedBy">The user who last modified it, or <c>null</c> if it never has been modified.</param>
public record GetProductResult(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    Money Price,
    Guid? CategoryId,
    ProductStatus Status,
    Guid TenantId,
    string? TenantCurrency,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn,
    Guid? CreatedBy,
    Guid? LastModifiedBy);
