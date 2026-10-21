using ShopIt.Catalog.Domain.Categories;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Catalog.Domain.Events;

/// <summary>
/// Raised when a category has been created.
/// </summary>
/// <remarks>
/// No handler consumes it yet. It is raised anyway because the aggregate's vocabulary is part of its
/// design and because it is the anchor the first integration event (publishing the category to a
/// storefront, say) will hang off — the alternative is every future consumer reaching into the entity
/// instead. Dispatch costs nothing when no handler is registered.
/// </remarks>
public record CategoryCreatedDomainEvent(Category Category) : DomainEvent;
