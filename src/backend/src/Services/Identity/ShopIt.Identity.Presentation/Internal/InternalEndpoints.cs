using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ShopIt.Identity.Application.Contracts.Models;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Presentation.Internal;

public static class InternalEndpoints
{
    /// <summary>
    /// Policy restricting internal endpoints to the backend client-credentials token
    /// (<c>shopit-backend</c>), so interactive user tokens cannot call them.
    /// </summary>
    public const string InternalPolicyName = "InternalOnly";

    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal")
            .RequireAuthorization(InternalPolicyName)
            .WithTags("Internal");

        // Only interactive request/response operations live here. Password reset and
        // email confirmation flows are event-driven via the Kafka outbox/inbox pattern.
        group.MapPost("/validate-credentials", ValidateCredentials);

        // Lets other backend services (e.g. Tenancy) enforce Identity's permission model
        // without exposing user tokens. Callable only with service/client credentials.
        group.MapGet("/users/{userId:guid}/permissions", GetUserPermissionsInternal);

        return app;
    }

    private static async Task<IResult> GetUserPermissionsInternal(
        Guid userId,
        UserManager<User> userManager,
        IPermissionResolver permissionResolver,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Results.NotFound();
        }

        var permissions = await permissionResolver.GetGrantedPermissionsAsync(user, cancellationToken);
        return Results.Ok(new { permissions = permissions.ToList() });
    }

    private static async Task<IResult> ValidateCredentials(
        [FromBody] CredentialValidationRequest request,
        UserManager<User> userManager,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Username and password are required.");
        }

        var user = await userManager.FindByNameAsync(request.Username);

        if (user is null)
        {
            return Results.NotFound();
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            return Results.Unauthorized();
        }

        var result = new CredentialValidationResponse(
            user.Id,
            user.TenantId,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed);

        return Results.Ok(result);
    }
}
