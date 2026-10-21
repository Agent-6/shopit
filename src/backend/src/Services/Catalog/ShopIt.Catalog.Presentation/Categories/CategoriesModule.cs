using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Catalog.Application.Categories.Commands.CreateCategory;
using ShopIt.Catalog.Application.Categories.Commands.DeleteCategory;
using ShopIt.Catalog.Application.Categories.Commands.RenameCategory;
using ShopIt.Catalog.Application.Categories.Queries.GetCategories;
using ShopIt.Catalog.Application.Categories.Queries.GetCategory;
using ShopIt.Catalog.Domain.Permissions;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Framework.Presentation.Modules;
using ShopIt.Framework.Presentation.Results;

namespace ShopIt.Catalog.Presentation.Categories;

/// <summary>
/// The catalog's category endpoints.
/// </summary>
/// <remarks>
/// <para>
/// Every route is permission-gated: <c>catalog.view</c> for reads, <c>catalog.write</c> for writes. The
/// gates are declared per endpoint rather than on the group so adding a route without one is visible in
/// the diff instead of inherited silently.
/// </para>
/// <para>
/// <c>DELETE</c> refuses with a 409 while products still reference the category: a cascade would delete a
/// tenant's products along with the shelf they sit on. That decision was taken with the products issue (#78)
/// and lives in <c>DeleteCategoryCommandHandler</c>.
/// </para>
/// </remarks>
public class CategoriesModule : EndpointsModule
{
    /// <summary>The page size used when a client does not ask for one.</summary>
    private const int DefaultPageSize = 20;

    public override string GroupDisplayName => "Categories";

    public override RoutePattern GroupPrefix => RoutePatternFactory.Parse("/categories");

    public override void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetCategories).RequirePermission(ShopItCatalogPermissions.View);
        app.MapGet("/{categoryId:guid}", GetCategoryById).RequirePermission(ShopItCatalogPermissions.View);
        app.MapPost("/", CreateCategory).RequirePermission(ShopItCatalogPermissions.Write);
        app.MapPut("/{categoryId:guid}", RenameCategory).RequirePermission(ShopItCatalogPermissions.Write);
        app.MapDelete("/{categoryId:guid}", DeleteCategory).RequirePermission(ShopItCatalogPermissions.Write);
    }

    private async Task<IResult> GetCategories(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] string? filter,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        // Paging is defaulted rather than required so a client can ask for "the list" without knowing
        // this service's conventions; the bounds themselves are validated by the handler, which returns a
        // Result like every other expected failure.
        var query = new GetCategoriesQuery(
            pageNumber ?? 1,
            pageSize ?? DefaultPageSize,
            filter);

        var result = await dispatcher.QueryAsync(query, cancellationToken);

        return result.ToHttpResult();
    }

    private async Task<IResult> GetCategoryById(
        Guid categoryId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(new GetCategoryQuery(categoryId), cancellationToken);

        return result.ToHttpResult();
    }

    private async Task<IResult> CreateCategory(
        CreateCategoryRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(new CreateCategoryCommand(request.Name), cancellationToken);

        return result.ToHttpResult(created => Results.Created($"/categories/{created.Id}", created));
    }

    private async Task<IResult> RenameCategory(
        Guid categoryId,
        RenameCategoryRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(
            new RenameCategoryCommand(categoryId, request.Name),
            cancellationToken);

        return result.ToHttpResult();
    }

    private async Task<IResult> DeleteCategory(
        Guid categoryId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(new DeleteCategoryCommand(categoryId), cancellationToken);

        // 204: the resource is gone, so there is nothing to describe. A 200 with an empty body would suggest
        // a representation exists.
        return result.ToHttpResult(StatusCodes.Status204NoContent);
    }
}

/// <summary>The body of a create-category request.</summary>
public record CreateCategoryRequest(string Name);

/// <summary>The body of a rename-category request.</summary>
public record RenameCategoryRequest(string Name);
