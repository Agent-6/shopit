using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Modules;
using ShopIt.Tenancy.Presentation.Authorization;
using ShopIt.Tenancy.Application.Tenants.Commands.ActivateTenant;
using ShopIt.Tenancy.Application.Tenants.Commands.CreateTenant;
using ShopIt.Tenancy.Application.Tenants.Commands.DeactivateTenant;
using ShopIt.Tenancy.Application.Tenants.Commands.DeleteTenant;
using ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;
using ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;
using ShopIt.Tenancy.Application.Tenants.Queries.GetTenants;

namespace ShopIt.Tenancy.Presentation.Tenants;

public class TenantsModule : EndpointsModule
{
    public override string GroupDisplayName => "Tenants";
    public override RoutePattern GroupPrefix => RoutePatternFactory.Parse("/tenants");

    public override void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetTenants).RequirePermission(ShopItTenancyPermissions.View);
        app.MapGet("/{tenantId:guid}", GetTenantById).RequirePermission(ShopItTenancyPermissions.View);
        app.MapPost("/", CreateTenant).RequirePermission(ShopItTenancyPermissions.Create);
        app.MapPut("/{tenantId:guid}", UpdateTenant).RequirePermission(ShopItTenancyPermissions.Update);
        app.MapDelete("/{tenantId:guid}", DeleteTenant).RequirePermission(ShopItTenancyPermissions.Delete);
        app.MapPut("/{tenantId:guid}/activate", ActivateTenant).RequirePermission(ShopItTenancyPermissions.ActivateDeactivate);
        app.MapPut("/{tenantId:guid}/deactivate", DeactivateTenant).RequirePermission(ShopItTenancyPermissions.ActivateDeactivate);
    }

    private async Task<IResult> GetTenants(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] string? filter,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(new GetTenantsQuery(pageNumber, pageSize, filter), cancellationToken);
        return Results.Ok(result);
    }

    private async Task<IResult> GetTenantById(
        Guid tenantId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(new GetTenantQuery(tenantId), cancellationToken);
        return Results.Ok(result);
    }

    private async Task<IResult> CreateTenant(
        CreateTenantRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var cmd = new CreateTenantCommand(request.Name);
        var result = await dispatcher.SendAsync(cmd, cancellationToken);
        return Results.Created($"/tenants/{result.Id}", result);
    }

    private async Task<IResult> UpdateTenant(
        Guid tenantId,
        UpdateTenantRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var cmd = new UpdateTenantCommand(tenantId, request.Name);
        var result = await dispatcher.SendAsync(cmd, cancellationToken);
        return Results.Ok(result);
    }

    private async Task<IResult> DeleteTenant(
        Guid tenantId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var cmd = new DeleteTenantCommand(tenantId);
        var result = await dispatcher.SendAsync(cmd, cancellationToken);
        return Results.Ok(result);
    }

    private async Task<IResult> ActivateTenant(
        Guid tenantId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var cmd = new ActivateTenantCommand(tenantId);
        var result = await dispatcher.SendAsync(cmd, cancellationToken);
        return Results.Ok(result);
    }

    private async Task<IResult> DeactivateTenant(
        Guid tenantId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var cmd = new DeactivateTenantCommand(tenantId);
        var result = await dispatcher.SendAsync(cmd, cancellationToken);
        return Results.Ok(result);
    }
}

public record CreateTenantRequest(string Name);
public record UpdateTenantRequest(string Name);
