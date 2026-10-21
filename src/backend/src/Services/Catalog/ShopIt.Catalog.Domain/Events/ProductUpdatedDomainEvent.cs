using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Catalog.Domain.Events;

/// <summary>
/// Raised when a product's editable details have changed.
/// </summary>
/// <remarks>
/// Carries the aggregate rather than the old and new values, so a future handler reads the product's
/// current state instead of reconstructing it from snapshots that may already be stale. A status change is
/// a separate event on purpose: a consumer that only publishes to a storefront cares about one and not the
/// other.
/// </remarks>
public record ProductUpdatedDomainEvent(Product Product) : DomainEvent;
