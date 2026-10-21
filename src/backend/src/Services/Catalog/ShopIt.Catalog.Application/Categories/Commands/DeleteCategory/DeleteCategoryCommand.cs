using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Categories.Commands.DeleteCategory;

/// <summary>
/// Deletes a category in the acting tenant.
/// </summary>
/// <remarks>
/// Refused while products still reference the category: a delete that cascaded would take a tenant's products
/// with it, and one that orphaned them would leave products filed under a category nobody can name. The
/// caller moves or deletes the products first, which is a decision this service should not make for them.
/// </remarks>
/// <param name="Id">The category to delete.</param>
public record DeleteCategoryCommand(Guid Id) : ICommand<Result<Unit, Error>>;
