using ShopIt.Catalog.Domain.Categories;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Catalog.Domain.Events;

/// <summary>
/// Raised when a category has been renamed.
/// </summary>
/// <remarks>
/// Carries the aggregate rather than the old and new names, so a future handler reads the category's
/// current state instead of reconstructing it from two string snapshots that may already be stale.
/// </remarks>
public record CategoryRenamedDomainEvent(Category Category) : DomainEvent;
