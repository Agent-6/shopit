using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Application.Products.Commands.ChangeProductStatus;

/// <summary>
/// The product after the status change.
/// </summary>
/// <param name="Id">The product's identifier.</param>
/// <param name="PreviousStatus">The status it moved out of; equal to <paramref name="Status"/> for a no-op.</param>
/// <param name="Status">Its status now.</param>
/// <param name="Changed">
/// Whether this request changed anything. <c>false</c> for a no-op — the product already had the requested
/// status — so a client can tell "done" from "was already done".
/// </param>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="LastModifiedOn">When it was last modified, stamped after this handler returns.</param>
public record ChangeProductStatusResult(
    Guid Id,
    ProductStatus PreviousStatus,
    ProductStatus Status,
    bool Changed,
    Guid TenantId,
    DateTimeOffset? LastModifiedOn);
