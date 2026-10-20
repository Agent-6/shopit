using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Framework.Presentation.Modules;
using ShopIt.Framework.Presentation.Results;
using ShopIt.Identity.Domain.Permissions;
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

        // Role permissions (manages permission claims without touching custom claims)
        app.MapPut("/{roleId:guid}/permissions", UpdateRolePermissions).RequirePermission(ShopItIdentityPermissions.Roles.ManagePermissions);
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
            Items = result.Roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description, r.CreatedAt, r.MultiTenancySide)).ToList(),
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

        if (role.IsFailure)
        {
            return role.Error.ToProblem();
        }

        var claims = await dispatcher.QueryAsync(
            new ShopIt.Identity.Application.Roles.Queries.GetRoleClaims.GetRoleClaimsQuery(roleId),
            cancellationToken);

        return claims.ToHttpResult(
            onSuccess: detail => Results.Ok(new RoleDetailResponse(
                role.Value.Id,
                role.Value.Name,
                role.Value.Description,
                role.Value.CreatedAt,
                role.Value.MultiTenancySide,
                detail.Claims.Select(c => new RoleClaimResponse(c.Type, c.Value)).ToList())));
    }

    private async Task<IResult> CreateRole(CreateRoleRequest request, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.CreateRole.CreateRoleCommand(
                request.Name, request.Description, request.MultiTenancySide),
            cancellationToken);

        return result.ToHttpResult(
            onSuccess: role => Results.Created(
                $"/roles/{role.Id}",
                new CreateRoleResponse(role.Id, role.Name, role.Description, role.CreatedAt, role.MultiTenancySide)));
    }

    private async Task<IResult> UpdateRole(Guid roleId, UpdateRoleRequest request, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.UpdateRole.UpdateRoleCommand(roleId, request.Name, request.Description),
            cancellationToken);

        return result.ToHttpResult(
            onSuccess: role => Results.Ok(
                new UpdateRoleResponse(role.Id, role.Name, role.Description, role.UpdatedAt)));
    }

    private async Task<IResult> DeleteRole(Guid roleId, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.DeleteRole.DeleteRoleCommand(roleId),
            cancellationToken);

        return result.ToHttpResult(
            onSuccess: deleted => Results.Ok(new DeleteRoleResponse(deleted.Id, deleted.IsDeleted)));
    }

    private async Task<IResult> UpdateRolePermissions(Guid roleId, UpdateRolePermissionsRequest request, IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var items = request.Permissions.Select(p => new ShopIt.Identity.Application.Roles.Commands.UpdateRolePermissions.PermissionUpdateItem(p.PermissionName, p.IsGranted));
        var result = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Roles.Commands.UpdateRolePermissions.UpdateRolePermissionsCommand(roleId, items),
            cancellationToken);

        return result.ToHttpResult(
            onSuccess: updated => Results.Ok(new UpdateRolePermissionsResponse(
                updated.RoleId,
                updated.GrantedPermissions.ToList(),
                updated.RevokedPermissions.ToList(),
                updated.UpdatedAt)));
    }
}
