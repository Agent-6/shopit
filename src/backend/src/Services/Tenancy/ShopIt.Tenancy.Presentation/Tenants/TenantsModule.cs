using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Modules;
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
        app.MapGet("/", GetTenants);
        app.MapGet("/{tenantId:guid}", GetTenantById);
        app.MapPost("/", CreateTenant);
        app.MapPut("/{tenantId:guid}", UpdateTenant);
        app.MapDelete("/{tenantId:guid}", DeleteTenant);
        app.MapPut("/{tenantId:guid}/activate", ActivateTenant);
        app.MapPut("/{tenantId:guid}/deactivate", DeactivateTenant);
    }

    private async Task<IResult> GetTenants(
        IDispatcher dispatcher,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(new GetTenantsQuery(page, pageSize, filter), cancellationToken);
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
        IDispatcher dispatcher = null!,
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
