using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Modules;
using ShopIt.Identity.Domain.Permissions;
using ShopIt.Identity.Presentation.Authorization;
using ShopIt.Identity.Presentation.Roles.Requests;
using ShopIt.Identity.Presentation.Roles.Responses;

namespace ShopIt.Identity.Presentation.Roles;

public class RolesModule : EndpointsModule
{
    public override string GroupDisplayName => "Roles";
    public override RoutePattern GroupPrefix => RoutePatternFactory.Parse("/roles");

    public override void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        // Role CRUD
        app.MapGet("/", GetRoles).RequirePermission(ShopItIdentityPermissions.Roles.View);
        app.MapGet("/{roleId:guid}", GetRoleById).RequirePermission(ShopItIdentityPermissions.Roles.View);
        app.MapPost("/", CreateRole).RequirePermission(ShopItIdentityPermissions.Roles.Create);
        app.MapPut("/{roleId:guid}", UpdateRole).RequirePermission(ShopItIdentityPermissions.Roles.Update);
        app.MapDelete("/{roleId:guid}", DeleteRole).RequirePermission(ShopItIdentityPermissions.Roles.Delete);

        // Role claims (permissions assigned to a role)
        app.MapGet("/{roleId:guid}/claims", GetRoleClaims).RequirePermission(ShopItIdentityPermissions.Roles.View);
        app.MapPut("/{roleId:guid}/claims", UpdateRoleClaims).RequirePermission(ShopItIdentityPermissions.Roles.ManagePermissions);
    }

    private async Task<IResult> GetRoles(
        IDispatcher dispatcher,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(
            new ShopIt.Identity.Application.Roles.Queries.GetRoles.GetRolesQuery(page, pageSize, filter),
            cancellationToken);

        var response = new GetRolesResponse
        {
            Items = result.Roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description, r.CreatedAt)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };

        return Results.Ok(response);
    }

    private async Task<IResult> GetRoleById(Guid roleId, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var role = await dispatcher.QueryAsync(
            new ShopIt.Identity.Application.Roles.Queries.GetRole.GetRoleQuery(roleId),
            cancellationToken);

        var claims = await dispatcher.QueryAsync(
            new ShopIt.Identity.Application.Roles.Queries.GetRoleClaims.GetRoleClaimsQuery(roleId),
            cancellationToken);

        var response = new RoleDetailResponse(
            role.Id,
            role.Name,
            role.Description,
            role.CreatedAt,
            claims.Claims.Select(c => new RoleClaimResponse(c.Type, c.Value)).ToList()
        );

        return Results.Ok(response);
    }

    private async Task<IResult> CreateRole(CreateRoleRequest request, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.CreateRole.CreateRoleCommand(request.Name, request.Description),
            cancellationToken);

        var response = new CreateRoleResponse(result.Id, result.Name, result.Description, result.CreatedAt);
        return Results.Created($"/roles/{response.Id}", response);
    }

    private async Task<IResult> UpdateRole(Guid roleId, UpdateRoleRequest request, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.UpdateRole.UpdateRoleCommand(roleId, request.Name, request.Description),
            cancellationToken);

        var response = new UpdateRoleResponse(result.Id, result.Name, result.Description, result.UpdatedAt);
        return Results.Ok(response);
    }

    private async Task<Results<Ok<DeleteRoleResponse>, BadRequest>> DeleteRole(Guid roleId, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.DeleteRole.DeleteRoleCommand(roleId),
            cancellationToken);

        return TypedResults.Ok(new DeleteRoleResponse(result.Id, result.IsDeleted));
    }

    private async Task<IResult> GetRoleClaims(Guid roleId, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(
            new ShopIt.Identity.Application.Roles.Queries.GetRoleClaims.GetRoleClaimsQuery(roleId),
            cancellationToken);

        var claims = result.Claims.Select(c => new RoleClaimResponse(c.Type, c.Value)).ToList();
        return Results.Ok(new GetRoleClaimsResponse(roleId, claims));
    }

    private async Task<IResult> UpdateRoleClaims(Guid roleId, UpdateRoleClaimsRequest request, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var claims = request.Claims.Select(c => new ShopIt.Identity.Application.Roles.Commands.UpdateRoleClaims.RoleClaimUpdateItem(c.ClaimType, c.ClaimValue));
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.UpdateRoleClaims.UpdateRoleClaimsCommand(roleId, claims),
            cancellationToken);

        var response = new UpdateRoleClaimsResponse(
            result.RoleId,
            result.Claims.Select(c => new RoleClaimResponse(c.Type, c.Value)).ToList(),
            result.UpdatedAt);
        return Results.Ok(response);
    }
}
