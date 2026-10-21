using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Catalog.Domain.Events;

/// <summary>
/// Raised when a product has moved between lifecycle statuses.
/// </summary>
/// <remarks>
/// The transition is carried explicitly — <see cref="From"/> and <see cref="To"/> — because the interesting
/// fact is the direction: activating and archiving are the same shape of event to the lifecycle and opposite
/// facts to a consumer.
/// </remarks>
/// <param name="Product">The product, in its new state.</param>
/// <param name="From">The status it moved out of.</param>
/// <param name="To">The status it moved into.</param>
public record ProductStatusChangedDomainEvent(Product Product, ProductStatus From, ProductStatus To) : DomainEvent;
