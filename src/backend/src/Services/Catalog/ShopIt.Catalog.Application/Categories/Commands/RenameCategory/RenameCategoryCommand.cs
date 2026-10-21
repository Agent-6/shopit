using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Categories.Commands.RenameCategory;

/// <summary>
/// Renames a category in the acting tenant.
/// </summary>
/// <param name="Id">The category to rename.</param>
/// <param name="Name">The new display name; trimmed and validated by the handler.</param>
public record RenameCategoryCommand(Guid Id, string Name) : ICommand<Result<RenameCategoryResult, Error>>;
