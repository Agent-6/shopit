using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Modules;
using ShopIt.Identity.Domain.Permissions;
using ShopIt.Identity.Presentation.Authorization;
using ShopIt.Identity.Presentation.Permissions.Responses;

namespace ShopIt.Identity.Presentation.Permissions;

public class PermissionsModule : EndpointsModule
{
    public override string GroupDisplayName => "Permissions";
    public override RoutePattern GroupPrefix => RoutePatternFactory.Parse("/permissions");

    public override void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        // Returns the permission catalog (groups + definitions) so permission management
        // UIs can render the full matrix of grantable permissions.
        app.MapGet("/", GetPermissionDefinitions).RequirePermission(ShopItIdentityPermissions.Roles.View);

        // Returns the roles × permissions matrix (catalog + every role's claims) for
        // permission matrix UIs. Requires role permission management since it is a
        // management tool backed by role claim edits.
        app.MapGet("/matrix", GetPermissionMatrix).RequirePermission(ShopItIdentityPermissions.Roles.ManagePermissions);
    }

    private static async Task<IResult> GetPermissionMatrix(
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(
            new ShopIt.Identity.Application.Permissions.Queries.GetPermissionMatrix.GetPermissionMatrixQuery(),
            cancellationToken);

        var response = new GetPermissionMatrixResponse(
            result.Groups.Select(g => new PermissionGroupResponse(
                g.Name,
                g.DisplayName,
                g.Permissions.Select(p => new PermissionDefinitionResponse(p.Name, p.DisplayName, p.Description)).ToList()
            )).ToList(),
            result.Roles.Select(r => new PermissionMatrixRoleResponse(
                r.Id,
                r.Name,
                r.Claims.Select(c => new PermissionMatrixClaimResponse(c.Type, c.Value)).ToList()
            )).ToList()
        );

        return Results.Ok(response);
    }

    private static IResult GetPermissionDefinitions(IPermissionDefinitionProvider provider)
    {
        var groups = provider.GetGroups().Select(g => new PermissionGroupResponse(
            g.Name,
            g.DisplayName,
            g.Permissions.Select(p => new PermissionDefinitionResponse(p.Name, p.DisplayName, p.Description)).ToList()
        )).ToList();

        return Results.Ok(new GetPermissionDefinitionsResponse(groups));
    }
}
