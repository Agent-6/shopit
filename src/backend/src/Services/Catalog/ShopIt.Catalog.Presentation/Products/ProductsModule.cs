using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Catalog.Application.Products.Commands.ChangeProductStatus;
using ShopIt.Catalog.Application.Products.Commands.CreateProduct;
using ShopIt.Catalog.Application.Products.Commands.UpdateProduct;
using ShopIt.Catalog.Application.Products.Queries.GetProduct;
using ShopIt.Catalog.Application.Products.Queries.GetProducts;
using ShopIt.Catalog.Domain.Permissions;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Framework.Presentation.Modules;
using ShopIt.Framework.Presentation.Results;

namespace ShopIt.Catalog.Presentation.Products;

/// <summary>
/// The catalog's product endpoints: the paged, filterable list and the detail read, plus the write commands.
/// </summary>
/// <remarks>
/// <para>
/// Reads require <c>catalog.view</c> and writes <c>catalog.write</c>, declared per endpoint rather than on the
/// group so a new route without a gate is visible in the diff instead of inherited silently.
/// </para>
/// <para>
/// Paging and sorting are validated by the handler, not here: <c>page</c> and <c>pageSize</c> default so a
/// client can ask for "the list" without knowing this service's conventions, but an out-of-range page size or
/// an unknown <c>sortBy</c>/<c>sortOrder</c> comes back as the handler's <c>Result</c> failure — a 400 — rather
/// than a silent clamp or a default order.
/// </para>
/// <para>
/// The price is <c>{ "amount": "19.99", "currency": "EUR" }</c> in both directions, with the amount as a
/// <em>string</em>: that is ADR-0005's contract, and it is why the read DTOs carry <c>Money</c> and not a
/// <c>decimal</c>. A JavaScript client that reads a price, lets a user edit it and posts it back never turns
/// the amount into a double, which is exactly the rounding the ADR exists to prevent.
/// </para>
/// </remarks>
public class ProductsModule : EndpointsModule
{
    /// <summary>The page size used when a client does not ask for one.</summary>
    private const int DefaultPageSize = 20;

    public override string GroupDisplayName => "Products";

    public override RoutePattern GroupPrefix => RoutePatternFactory.Parse("/products");

    public override void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetProducts).RequirePermission(ShopItCatalogPermissions.View);
        app.MapGet("/{productId:guid}", GetProductById).RequirePermission(ShopItCatalogPermissions.View);
        app.MapPost("/", CreateProduct).RequirePermission(ShopItCatalogPermissions.Write);
        app.MapPut("/{productId:guid}", UpdateProduct).RequirePermission(ShopItCatalogPermissions.Write);

        // One status route with a target status, mirroring the command: the lifecycle's rule is about
        // transitions, and two routes (activate/archive) would encode the same rule twice in the HTTP surface.
        app.MapPut("/{productId:guid}/status", ChangeProductStatus).RequirePermission(ShopItCatalogPermissions.Write);
    }

    private async Task<IResult> GetProducts(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? filter,
        [FromQuery] Guid? categoryId,
        [FromQuery] ProductStatus? status,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        // Only the paging defaults live here; the bounds and the sort vocabulary are the handler's, so they
        // are reported as Result failures rather than being papered over at the edge.
        var result = await dispatcher.QueryAsync(
            new GetProductsQuery(
                page ?? 1,
                pageSize ?? DefaultPageSize,
                filter,
                categoryId,
                status,
                sortBy,
                sortOrder),
            cancellationToken);

        return result.ToHttpResult();
    }

    private async Task<IResult> GetProductById(
        Guid productId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(new GetProductQuery(productId), cancellationToken);

        return result.ToHttpResult();
    }

    private async Task<IResult> CreateProduct(
        CreateProductRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (!TryReadPrice(request.Price, out var amount, out var currency))
        {
            return ProductErrors.PriceRequired().ToProblem();
        }

        var result = await dispatcher.SendAsync(
            new CreateProductCommand(
                request.Name,
                request.Sku,
                request.Description,
                amount,
                currency,
                request.CategoryId),
            cancellationToken);

        return result.ToHttpResult(created => Results.Created($"/products/{created.Id}", created));
    }

    private async Task<IResult> UpdateProduct(
        Guid productId,
        UpdateProductRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (!TryReadPrice(request.Price, out var amount, out var currency))
        {
            return ProductErrors.PriceRequired().ToProblem();
        }

        var result = await dispatcher.SendAsync(
            new UpdateProductCommand(
                productId,
                request.Name,
                request.Sku,
                request.Description,
                amount,
                currency,
                request.CategoryId),
            cancellationToken);

        return result.ToHttpResult();
    }

    private async Task<IResult> ChangeProductStatus(
        Guid productId,
        ChangeProductStatusRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (request.Status is not { } status)
        {
            return ProductErrors.StatusRequired().ToProblem();
        }

        var result = await dispatcher.SendAsync(
            new ChangeProductStatusCommand(productId, status),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// Reads a request price, or reports that it is missing or not a decimal written with the invariant
    /// culture.
    /// </summary>
    /// <remarks>
    /// Parsing happens here, not in the domain: "is this text a decimal" is a wire-format question, while
    /// "is this a valid price" (non-negative, two decimals, ISO currency) is the aggregate's and stays in
    /// <see cref="Money"/>. The result is a <c>decimal</c>, so the value the domain validates is the value the
    /// client typed — no double in between.
    /// </remarks>
    private static bool TryReadPrice(ProductPriceRequest? price, out decimal amount, out string? currency)
    {
        amount = 0m;
        currency = price?.Currency;

        return price is not null
               && decimal.TryParse(
                   price.Amount,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out amount);
    }
}

/// <summary>A price in a request body, shaped like the response: the amount is a string.</summary>
/// <param name="Amount">The amount, for example <c>"19.99"</c>.</param>
/// <param name="Currency">
/// The ISO 4217 code. Optional while the catalog prices in one platform-wide currency (#84); when supplied it
/// must match, or the request is refused.
/// </param>
public record ProductPriceRequest(string? Amount, string? Currency);

/// <summary>The body of a create-product request.</summary>
/// <param name="Name">The display name.</param>
/// <param name="Sku">The stock-keeping unit, unique within the tenant.</param>
/// <param name="Description">An optional description.</param>
/// <param name="Price">The price.</param>
/// <param name="CategoryId">An optional category, which must exist in the acting tenant.</param>
public record CreateProductRequest(
    string? Name,
    string? Sku,
    string? Description,
    ProductPriceRequest? Price,
    Guid? CategoryId);

/// <summary>The body of an update-product request. It replaces every editable field.</summary>
/// <param name="Name">The display name.</param>
/// <param name="Sku">The stock-keeping unit, unique within the tenant.</param>
/// <param name="Description">The description, or <c>null</c> to remove it.</param>
/// <param name="Price">The price.</param>
/// <param name="CategoryId">The category, or <c>null</c> to make the product uncategorised.</param>
public record UpdateProductRequest(
    string? Name,
    string? Sku,
    string? Description,
    ProductPriceRequest? Price,
    Guid? CategoryId);

/// <summary>The body of a status-change request.</summary>
/// <param name="Status">The status to move to: <c>Draft</c>, <c>Active</c> or <c>Archived</c>.</param>
public record ChangeProductStatusRequest(ProductStatus? Status);
