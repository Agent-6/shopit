using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Catalog.Application.Categories.Commands.CreateCategory;

/// <summary>
/// Creates a category in the acting tenant.
/// </summary>
/// <param name="Name">The display name; trimmed and validated by the handler.</param>
public record CreateCategoryCommand(string Name) : ICommand<Result<CreateCategoryResult, Error>>;
