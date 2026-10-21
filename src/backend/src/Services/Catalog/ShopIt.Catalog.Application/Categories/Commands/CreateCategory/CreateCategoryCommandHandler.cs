using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Categories.Commands.CreateCategory;

/// <summary>
/// Handles <see cref="CreateCategoryCommand"/>: tenant scope, name validation, per-tenant uniqueness,
/// then the aggregate is added and the persistence convention stamps who created it.
/// </summary>
public class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICurrentTenant currentTenant)
    : ICommandHandler<CreateCategoryCommand, Result<CreateCategoryResult, Error>>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<CreateCategoryResult, Error>> HandleAsync(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        // Host scope means no tenant claim, which also disables the query filter: writing here would
        // persist a row nobody owns, and reading would expose every tenant's catalog (ADR-0001).
        if (_currentTenant.IsHost)
        {
            return CategoryErrors.TenantRequired();
        }

        if (Category.ValidateName(request.Name) is { } validationError)
        {
            return validationError;
        }

        // Checked before the insert so the client gets a Conflict rather than a unique-constraint
        // exception. The index still backs it up: this check races with a concurrent create.
        if (await _categoryRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists(request.Name);
        }

        var category = Category.Create(Guid.NewGuid(), _currentTenant.Id, request.Name);

        // Audit fields are stamped from the acting user by the persistence path (ADR-0004); the handler
        // assigns none of them.
        await _categoryRepository.AddAsync(category, cancellationToken);

        return new CreateCategoryResult(
            Id: category.Id,
            Name: category.Name,
            TenantId: category.TenantId,
            CreatedOn: category.CreatedOn);
    }
}
