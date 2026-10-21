using Microsoft.Extensions.Options;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Products.Commands.CreateProduct;

/// <summary>
/// Handles <see cref="CreateProductCommand"/>: tenant scope, field and price validation, per-tenant SKU
/// uniqueness, the category's existence in this tenant, then the aggregate is added and the persistence
/// convention stamps who created it.
/// </summary>
public class CreateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ICurrentTenant currentTenant,
    IOptions<CatalogOptions> options)
    : ICommandHandler<CreateProductCommand, Result<CreateProductResult, Error>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IOptions<CatalogOptions> _options = options;

    public async Task<Result<CreateProductResult, Error>> HandleAsync(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // Host scope means no tenant claim, which also disables the query filter: writing here would persist
        // a row nobody owns, and reading would expose every tenant's catalog (ADR-0001).
        if (_currentTenant.IsHost)
        {
            return ProductErrors.TenantRequired();
        }

        // Each field is checked through the aggregate's own rule, so the message the client gets is the one
        // the aggregate would have thrown.
        if (Product.ValidateName(request.Name) is { } nameError)
        {
            return nameError;
        }

        if (Product.ValidateSku(request.Sku) is { } skuError)
        {
            return skuError;
        }

        if (Product.ValidateDescription(request.Description) is { } descriptionError)
        {
            return descriptionError;
        }

        // An empty guid is a malformed field rather than a missing category: without this, "00000000-…"
        // would be reported as "this category does not exist", which is true but not the caller's mistake.
        if (request.CategoryId == Guid.Empty)
        {
            return ProductErrors.Invalid(
                "Product.CategoryIdEmpty",
                "A category id must be a non-empty GUID; omit it to create an uncategorised product.");
        }

        if (ProductPriceResolver.Resolve(
                request.Amount,
                request.Currency,
                _options.Value.NormalizedCurrency,
                out var price) is { } priceError)
        {
            return priceError;
        }

        // Checked before the insert so the client gets a Conflict rather than a unique-constraint exception.
        // The index still backs it up: this check races with a concurrent create.
        if (await _productRepository.ExistsBySkuAsync(request.Sku!, cancellationToken))
        {
            return ProductErrors.SkuAlreadyExists(request.Sku!);
        }

        // The category lookup runs under the same tenant filter as every other read, so another tenant's
        // category id resolves to null and can never be assigned.
        if (request.CategoryId is { } categoryId
            && await _categoryRepository.FindByIdAsync(categoryId, cancellationToken) is null)
        {
            return ProductErrors.CategoryNotFound(categoryId);
        }

        var product = Product.Create(
            Guid.NewGuid(),
            _currentTenant.Id,
            request.Name!,
            request.Sku!,
            request.Description,
            price,
            request.CategoryId);

        // Audit fields are stamped from the acting user by the persistence path (ADR-0004); the handler
        // assigns none of them.
        await _productRepository.AddAsync(product, cancellationToken);

        return new CreateProductResult(
            Id: product.Id,
            Name: product.Name,
            Sku: product.Sku,
            Description: product.Description,
            Price: product.Price,
            CategoryId: product.CategoryId,
            Status: product.Status,
            TenantId: product.TenantId,
            CreatedOn: product.CreatedOn);
    }
}
