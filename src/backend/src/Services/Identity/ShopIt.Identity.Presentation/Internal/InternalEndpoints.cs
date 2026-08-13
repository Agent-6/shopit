using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Identity.Application.Contracts.Models;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Application.Users.Commands.CompleteActivation;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Enums;

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

        // Called synchronously by the Authentication service when the invited user submits
        // their new password on the activation page. Validates the token, stores the
        // password, activates the account and returns the account so the caller can sign
        // the user in (zero extra login steps).
        group.MapPost("/activate-user", CompleteActivation);

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

        // Phase 4 safeguard: an invited user has no password yet, so this check must come
        // BEFORE password validation — otherwise the 401 would mask the real reason. A user
        // in PendingActivation (or a disabled account) gets a distinct failure so the Auth
        // UI can point them back to their inbox instead of a generic "invalid credentials".
        if (user.Status == UserStatus.PendingActivation || !user.IsActive)
        {
            var (errorCode, message) = user.Status == UserStatus.PendingActivation
                ? ("ACCOUNT_NOT_ACTIVATED", "User has not activated their account.")
                : ("ACCOUNT_DISABLED", "This account has been deactivated.");

            return Results.Ok(new CredentialValidationResponse(
                Success: false,
                ErrorCode: errorCode,
                Message: message,
                UserId: user.Id,
                TenantId: user.TenantId,
                UserName: user.UserName,
                Email: user.Email));
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            return Results.Unauthorized();
        }

        var result = new CredentialValidationResponse(
            Success: true,
            ErrorCode: null,
            Message: null,
            UserId: user.Id,
            TenantId: user.TenantId,
            UserName: user.UserName!,
            Email: user.Email!,
            EmailConfirmed: user.EmailConfirmed);

        return Results.Ok(result);
    }

    private static async Task<IResult> CompleteActivation(
        [FromBody] ActivateUserRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("UserId, token and password are required.");
        }

        var result = await dispatcher.SendAsync(
            new CompleteActivationCommand(request.UserId, request.Token, request.Password),
            cancellationToken);

        // Always 200 with the structured outcome — the caller distinguishes success via
        // Succeeded / ErrorCode rather than HTTP status codes.
        var response = new ActivateUserResponse(
            result.Succeeded,
            result.UserId,
            result.TenantId,
            result.UserName,
            result.Email,
            result.ErrorCode,
            result.Error);

        return Results.Ok(response);
    }
}
