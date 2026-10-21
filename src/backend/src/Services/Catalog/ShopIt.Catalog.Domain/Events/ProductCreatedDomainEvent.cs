using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Catalog.Domain.Events;

/// <summary>
/// Raised when a product has been created.
/// </summary>
/// <remarks>
/// No handler consumes it yet. It is raised anyway because the aggregate's vocabulary is part of its design
/// and because it is the anchor the catalog's first integration event will hang off (announcing a product
/// to a storefront, say). Dispatch costs nothing while no handler is registered.
/// </remarks>
public record ProductCreatedDomainEvent(Product Product) : DomainEvent;
