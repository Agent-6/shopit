using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Categories.Commands.RenameCategory;

/// <summary>
/// Handles <see cref="RenameCategoryCommand"/>: find the category, keep its name unique within the
/// tenant, then rename.
/// </summary>
public class RenameCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICurrentTenant currentTenant)
    : ICommandHandler<RenameCategoryCommand, Result<RenameCategoryResult, Error>>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<RenameCategoryResult, Error>> HandleAsync(
        RenameCategoryCommand request,
        CancellationToken cancellationToken)
    {
        // Writes are refused at host scope: there is no tenant to own the row (ADR-0001).
        if (_currentTenant.IsHost)
        {
            return CategoryErrors.TenantRequired();
        }

        if (Category.ValidateName(request.Name) is { } validationError)
        {
            return validationError;
        }

        // Find, not Get: another tenant's id is reported as not found rather than as an exception —
        // and because the tenant filter hides that row, the answer does not confirm it exists.
        var category = await _categoryRepository.FindByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(request.Id);
        }

        // Renaming to a different spelling of its own name is not a conflict with itself.
        var normalizedName = Category.NormalizeName(request.Name);
        if (!string.Equals(category.NormalizedName, normalizedName, StringComparison.Ordinal)
            && await _categoryRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists(request.Name);
        }

        category.Rename(request.Name);

        // Audit fields are stamped by the persistence path (ADR-0004), not here.
        await _categoryRepository.UpdateAsync(category, cancellationToken);

        return new RenameCategoryResult(
            Id: category.Id,
            Name: category.Name,
            TenantId: category.TenantId,
            CreatedOn: category.CreatedOn,
            LastModifiedOn: category.LastModifiedOn);
    }
}
