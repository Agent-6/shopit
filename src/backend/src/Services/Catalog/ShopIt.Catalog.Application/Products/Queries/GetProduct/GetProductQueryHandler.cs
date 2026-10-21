using Microsoft.Extensions.Logging;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Tenancy.Client.Services;

namespace ShopIt.Catalog.Application.Products.Queries.GetProduct;

/// <summary>
/// Handles <see cref="GetProductQuery"/>.
/// </summary>
/// <remarks>
/// <para>
/// A product id that belongs to another tenant reads as "not found", not "forbidden": the repository query runs under
/// the tenant filter, so the row is invisible and the answer must not confirm that the id exists elsewhere (ADR-0001).
/// The handler turns the repository's <c>null</c> into <see cref="ProductErrors.NotFound"/> — a domain failure with a
/// 404 mapping — rather than passing <c>null</c> to the response.
/// </para>
/// <para>
/// It also reports the tenant's <em>current</em> currency beside the price's stored one, so a reader can see that a
/// shop has switched without the price having been re-denominated. That lookup is best effort and is the one place on
/// a read path that may fail: the read still answers, <see cref="GetProductResult.TenantCurrency"/> is <c>null</c>, and
/// the failure is logged rather than swallowed — a read stamps nothing, so degrading is honest here in a way it is not
/// on a write, where a guessed currency would be a price in the wrong money (ADR-0006).
/// </para>
/// </remarks>
public class GetProductQueryHandler(
    IProductRepository productRepository,
    ICurrentTenant currentTenant,
    ITenantCurrencyProvider tenantCurrencyProvider,
    ILogger<GetProductQueryHandler> logger)
    : IQueryHandler<GetProductQuery, Result<GetProductResult, Error>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ITenantCurrencyProvider _tenantCurrencyProvider = tenantCurrencyProvider;
    private readonly ILogger<GetProductQueryHandler> _logger = logger;

    public async Task<Result<GetProductResult, Error>> HandleAsync(
        GetProductQuery request,
        CancellationToken cancellationToken)
    {
        // At host scope the tenant filter is disabled, so this read would answer with another tenant's
        // product. The catalog is tenant-only, so it is refused rather than answered.
        if (_currentTenant.IsHost)
        {
            return ProductErrors.TenantRequired();
        }

        var product = await _productRepository.FindByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

        return new GetProductResult(
            Id: product.Id,
            Name: product.Name,
            Sku: product.Sku,
            Description: product.Description,
            Price: product.Price,
            CategoryId: product.CategoryId,
            Status: product.Status,
            TenantId: product.TenantId,
            TenantCurrency: await ResolveTenantCurrencyAsync(cancellationToken),
            CreatedOn: product.CreatedOn,
            LastModifiedOn: product.LastModifiedOn,
            // The actor is a user id, not a name: resolving it to a display name would be an Identity
            // lookup, and this read must not depend on the caller also holding `user.view` (#81 reports
            // the id rather than pretending it knows who that is).
            CreatedBy: product.CreatedBy,
            LastModifiedBy: product.LastModifiedBy);
    }

    /// <summary>
    /// The acting tenant's currency, or <c>null</c> when the tenant registry could not be asked.
    /// </summary>
    /// <remarks>
    /// The failure is logged at warning level: a missing tenant currency in a product response is explainable to a
    /// caller only if it leaves a trace here, and logging it is what keeps "advisory field" from meaning "unnoticed
    /// outage".
    /// </remarks>
    private async Task<string?> ResolveTenantCurrencyAsync(CancellationToken cancellationToken)
    {
        var currency = await _tenantCurrencyProvider.GetCurrencyAsync(_currentTenant.Id, cancellationToken);

        if (currency.IsFailure)
        {
            _logger.LogWarning(
                "Reporting product detail without the tenant's current currency ({Code}): {Description}",
                currency.Error.Code, currency.Error.Description);

            return null;
        }

        return currency.Value;
    }
}
