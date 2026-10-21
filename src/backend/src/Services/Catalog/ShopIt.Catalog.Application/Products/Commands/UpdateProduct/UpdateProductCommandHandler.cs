using Microsoft.Extensions.Options;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Handles <see cref="UpdateProductCommand"/>: find the product, keep its SKU unique within the tenant,
/// validate the new details and price, check the category, then update.
/// </summary>
public class UpdateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ICurrentTenant currentTenant,
    IOptions<CatalogOptions> options)
    : ICommandHandler<UpdateProductCommand, Result<UpdateProductResult, Error>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IOptions<CatalogOptions> _options = options;

    public async Task<Result<UpdateProductResult, Error>> HandleAsync(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        // Writes are refused at host scope: there is no tenant to own the row (ADR-0001).
        if (_currentTenant.IsHost)
        {
            return ProductErrors.TenantRequired();
        }

        // Find, not Get: another tenant's id is reported as not found rather than as an exception — and
        // because the tenant filter hides that row, the answer does not confirm it exists.
        var product = await _productRepository.FindByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

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

        if (request.CategoryId == Guid.Empty)
        {
            return ProductErrors.Invalid(
                "Product.CategoryIdEmpty",
                "A category id must be a non-empty GUID; omit it to make the product uncategorised.");
        }

        if (ProductPriceResolver.Resolve(
                request.Amount,
                request.Currency,
                _options.Value.NormalizedCurrency,
                out var price) is { } priceError)
        {
            return priceError;
        }

        // Archive ends the lifecycle, so an archived product is not editable — the same rule
        // Product.UpdateDetails enforces, reported here as a Result instead of an exception.
        if (product.Status is ProductStatus.Archived)
        {
            return ProductErrors.ArchivedIsImmutable(product.Id);
        }

        // Re-submitting a different spelling of its own SKU is not a conflict with itself.
        var normalizedSku = Product.NormalizeSku(request.Sku!);
        if (!string.Equals(product.NormalizedSku, normalizedSku, StringComparison.Ordinal)
            && await _productRepository.ExistsBySkuAsync(request.Sku!, cancellationToken))
        {
            return ProductErrors.SkuAlreadyExists(request.Sku!);
        }

        // Same tenant-filtered lookup as create: another tenant's category resolves to null and is never
        // assigned to this product.
        if (request.CategoryId is { } categoryId
            && await _categoryRepository.FindByIdAsync(categoryId, cancellationToken) is null)
        {
            return ProductErrors.CategoryNotFound(categoryId);
        }

        product.UpdateDetails(
            request.Name!,
            request.Sku!,
            request.Description,
            price,
            request.CategoryId);

        // Audit fields are stamped by the persistence path (ADR-0004), not here.
        await _productRepository.UpdateAsync(product, cancellationToken);

        return new UpdateProductResult(
            Id: product.Id,
            Name: product.Name,
            Sku: product.Sku,
            Description: product.Description,
            Price: product.Price,
            CategoryId: product.CategoryId,
            Status: product.Status,
            TenantId: product.TenantId,
            CreatedOn: product.CreatedOn,
            LastModifiedOn: product.LastModifiedOn);
    }
}
