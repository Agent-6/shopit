using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Identity.Application.Contracts.Models;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Presentation.Internal;

public static class InternalEndpoints
{
    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal")
            .RequireAuthorization()
            .WithTags("Internal");

        group.MapPost("/validate-credentials", ValidateCredentials);

        return app;
    }

    private static async Task<IResult> ValidateCredentials(
        [FromBody] CredentialValidationRequest request,
        UserManager<User> userManager,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Username and password are required.");
        }

        // Add mocked user logic for now
        if (request.Username == "mock@user.com" && request.Password == "mockpassword")
        {
            // WATCH: empty guid for host user.
            var hostUser = new CredentialValidationResponse(Guid.NewGuid(), Guid.Empty, "Mock User", "mock@user.com");
            return Results.Ok(hostUser);
        }

        if (request.Username == "tenant@user.com" && request.Password == "mockpassword")
        {
            var tenantUser = new CredentialValidationResponse(Guid.NewGuid(), new Guid("B5D0C0E4-3A5B-4CDC-8D2A-7F1F6C9F5B4E"), "Tenant User", "tenant@user.com");
            return Results.Ok(tenantUser);
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
            user.Email!);

        return Results.Ok(result);
    }
}
