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
using ShopIt.Identity.Presentation.Users.Enums;
using ShopIt.Identity.Presentation.Users.Requests;
using ShopIt.Identity.Presentation.Users.Responses;

namespace ShopIt.Identity.Presentation.Users;

public class UsersModule : EndpointsModule
{
    public override string GroupDisplayName => "Users";
    public override RoutePattern GroupPrefix => RoutePatternFactory.Parse("/users");

    public override void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        // Current user (any authenticated user can read their own permissions)
        app.MapGet("/me/permissions", GetMyPermissions).RequireAuthorization();

        // User CRUD Operations
        app.MapGet("/", GetUsers).RequirePermission(ShopItIdentityPermissions.Users.View);
        app.MapGet("/{userId:guid}", GetUserById).RequirePermission(ShopItIdentityPermissions.Users.View);
        app.MapPost("/", CreateUser).RequirePermission(ShopItIdentityPermissions.Users.Create);
        app.MapPost("/invite", InviteUser).RequirePermission(ShopItIdentityPermissions.Users.Create);
        app.MapPut("/{userId:guid}", UpdateUser).RequirePermission(ShopItIdentityPermissions.Users.Update);
        app.MapDelete("/{userId:guid}", DeleteUser).RequirePermission(ShopItIdentityPermissions.Users.Delete);

        // User Permissions (reads are view-level so the user detail page renders; mutations require management)
        app.MapGet("/{userId:guid}/permissions", GetUserPermissions).RequirePermission(ShopItIdentityPermissions.Users.View);
        app.MapPut("/{userId:guid}/permissions", UpdateUserPermissions).RequirePermission(ShopItIdentityPermissions.Users.ManagePermissions);

        // User Claims (the catch-all claimValue segment tolerates '/' inside claim values)
        app.MapGet("/{userId:guid}/claims", GetUserClaims).RequirePermission(ShopItIdentityPermissions.Users.View);
        app.MapPut("/{userId:guid}/claims", UpdateUserClaims).RequirePermission(ShopItIdentityPermissions.Users.ManageClaims);
        app.MapDelete("/{userId:guid}/claims/{claimType}/{*claimValue}", RemoveUserClaim).RequirePermission(ShopItIdentityPermissions.Users.ManageClaims);

        // User Roles
        app.MapGet("/{userId:guid}/roles", GetUserRoles).RequirePermission(ShopItIdentityPermissions.Users.View);
        app.MapPut("/{userId:guid}/roles", UpdateUserRoles).RequirePermission(ShopItIdentityPermissions.Users.ManageRoles);

        // User Security & Status
        app.MapPost("/{userId:guid}/lock", LockUser).RequirePermission(ShopItIdentityPermissions.Users.LockUnlock);
        app.MapPost("/{userId:guid}/unlock", UnlockUser).RequirePermission(ShopItIdentityPermissions.Users.LockUnlock);
        app.MapPost("/{userId:guid}/activate", ActivateUser).RequirePermission(ShopItIdentityPermissions.Users.Update);
        app.MapPost("/{userId:guid}/deactivate", DeactivateUser).RequirePermission(ShopItIdentityPermissions.Users.Update);
        app.MapPut("/{userId:guid}/password", UpdateUserPassword).RequirePermission(ShopItIdentityPermissions.Users.ResetPassword);
    }

    private async Task<IResult> GetMyPermissions(ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await dispatcher.QueryAsync(
                new ShopIt.Identity.Application.Users.Queries.GetMyPermissions.GetMyPermissionsQuery(),
                cancellationToken);

            return Results.Ok(new GetMyPermissionsResponse(res.Permissions.ToList()));
        }
        catch (UnauthorizedAccessException)
        {
            // Authenticated but no interactive user (e.g. a client-credentials token).
            return Results.Unauthorized();
        }
    }

    private async Task<IResult> GetUsers(
        IDispatcher dispatcher,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? filter = null,
        [FromQuery] string? sortBy = "username",
        [FromQuery] string? sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUsers.GetUsersQuery(page, pageSize, filter, sortBy, sortOrder), cancellationToken);

        var response = new GetUsersResponse
        {
            Items = result.Users.Select(u => new UserResponse(
                u.Id,
                u.Username,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.Status,
                u.EmailConfirmed,
                u.PhoneNumber,
                u.PhoneNumberConfirmed,
                u.LockoutEnabled,
                u.LockoutEnd,
                u.CreatedAt,
                u.LastModifiedAt,
                u.Roles.ToList()
            )).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };

        return Results.Ok(response);
    }

    private async Task<IResult> GetUserById(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var user = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUser.GetUserQuery(userId), cancellationToken);
        var claims = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserClaims.GetUserClaimsQuery(userId), cancellationToken);
        var roles = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserRoles.GetUserRolesQuery(userId), cancellationToken);

        var response = new UserDetailResponse(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsActive: user.IsActive,
            Status: user.Status,
            EmailConfirmed: user.EmailConfirmed,
            PhoneNumber: user.PhoneNumber,
            PhoneNumberConfirmed: user.PhoneNumberConfirmed,
            TwoFactorEnabled: user.TwoFactorEnabled,
            LockoutEnabled: user.LockoutEnabled,
            LockoutEnd: user.LockoutEnd,
            CreatedAt: user.CreatedAt,
            LastModifiedAt: user.LastModifiedAt,
            Roles: roles.Roles.ToList(),
            Claims: claims.Claims.Select(c => new UserClaimResponse(c.Type, c.Value)).ToList()
        );

        return Results.Ok(response);
    }

    private async Task<IResult> CreateUser(CreateUserRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var cmd = new ShopIt.Identity.Application.Users.Commands.CreateUser.CreateUserCommand(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName ?? string.Empty,
            request.LastName ?? string.Empty,
            request.PhoneNumber,
            request.Roles,
            request.Claims?.Select(c => new ShopIt.Identity.Application.Users.Commands.CreateUser.CreateUserClaimItem(c.ClaimType, c.ClaimValue))
        );

        var result = await dispatcher.SendAsync(cmd, cancellationToken);

        var response = new CreateUserResponse(result.Id, result.Username, result.Email, DateTime.UtcNow);
        return Results.Created($"/users/{response.Id}", response);
    }

    private async Task<IResult> InviteUser(InviteUserRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var cmd = new ShopIt.Identity.Application.Users.Commands.InviteUser.InviteUserCommand(
            request.Email,
            request.FirstName ?? string.Empty,
            request.LastName ?? string.Empty,
            request.PhoneNumber,
            request.Roles,
            request.Claims?.Select(c => new ShopIt.Identity.Application.Users.Commands.CreateUser.CreateUserClaimItem(c.ClaimType, c.ClaimValue))
        );

        var result = await dispatcher.SendAsync(cmd, cancellationToken);

        var response = new InviteUserResponse(result.Id, result.Email, result.Status, result.InvitationExpiresAt);
        return Results.Created($"/users/{response.Id}", response);
    }

    private async Task<IResult> UpdateUser(Guid userId, UpdateUserRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var cmd = new ShopIt.Identity.Application.Users.Commands.UpdateUser.UpdateUserCommand(
            userId,
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.IsActive,
            request.Roles,
            request.Claims?.Select(c => new ShopIt.Identity.Application.Users.Commands.UpdateUser.UpdateUserClaimItem(c.ClaimType, c.ClaimValue)),
            request.EmailConfirmed
        );

        var res = await dispatcher.SendAsync(cmd, cancellationToken);

        var response = new UpdateUserResponse(
            Id: res.Id,
            Username: res.Username,
            Email: res.Email,
            FirstName: res.FirstName,
            LastName: res.LastName,
            PhoneNumber: res.PhoneNumber,
            IsActive: res.IsActive,
            LastModifiedAt: res.LastModifiedAt
        );

        return Results.Ok(response);
    }

    private async Task<Results<Ok<DeleteUserResponse>, BadRequest>> DeleteUser(Guid userId, [FromQuery] bool permanent = false, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher = null!, CancellationToken cancellationToken = default)
    {
        var cmd = new ShopIt.Identity.Application.Users.Commands.DeleteUser.DeleteUserCommand(userId, permanent);
        var res = await dispatcher.SendAsync(cmd, cancellationToken);
        return TypedResults.Ok(new DeleteUserResponse(res.Id, res.IsDeleted, res.DeletedType));
    }

    // ------------------------------------------------------------------
    // Permissions
    // ------------------------------------------------------------------

    private async Task<IResult> GetUserPermissions(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var perms = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserPermissions.GetUserPermissionsQuery(userId), cancellationToken);

        var permissions = perms.Permissions.Select(p => new UserPermissionResponse(p.PermissionName, p.IsGranted, p.Source == "Role" ? PermissionSource.Role : PermissionSource.Direct)).ToList();
        var inherited = perms.InheritedPermissions.Select(ip => new InheritedPermissionResponse(ip.Permission, ip.Source)).ToList();

        return Results.Ok(new GetUserPermissionsResponse(userId, permissions, inherited));
    }

    private async Task<IResult> UpdateUserPermissions(Guid userId, UpdateUserPermissionsRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var items = request.Permissions.Select(p => new ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions.PermissionUpdateItem(p.PermissionName, p.IsGranted));
        var cmd = new ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions.UpdateUserPermissionsCommand(userId, items);
        var res = await dispatcher.SendAsync(cmd, cancellationToken);

        return Results.Ok(new UpdateUserPermissionsResponse(res.UserId, res.GrantedPermissions.ToList(), res.RevokedPermissions.ToList(), res.UpdatedAt));
    }

    // ------------------------------------------------------------------
    // Claims
    // ------------------------------------------------------------------

    private async Task<IResult> GetUserClaims(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserClaims.GetUserClaimsQuery(userId), cancellationToken);
        var claims = res.Claims.Select(c => new UserClaimResponse(c.Type, c.Value)).ToList();
        return Results.Ok(new GetUserClaimsResponse(userId, claims));
    }

    private async Task<IResult> UpdateUserClaims(Guid userId, UpdateUserClaimsRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var claims = request.Claims.Select(c => new ShopIt.Identity.Application.Users.Commands.UpdateUserClaims.UserClaimUpdateItem(c.ClaimType, c.ClaimValue));
        var removed = request.RemovedClaims?.Select(rc => new ShopIt.Identity.Application.Users.Commands.UpdateUserClaims.UserClaimUpdateItem(rc.Split(':')[0], rc.Split(':').ElementAtOrDefault(1) ?? string.Empty)) ?? Enumerable.Empty<ShopIt.Identity.Application.Users.Commands.UpdateUserClaims.UserClaimUpdateItem>();

        var cmd = new ShopIt.Identity.Application.Users.Commands.UpdateUserClaims.UpdateUserClaimsCommand(userId, claims, removed);
        var res = await dispatcher.SendAsync(cmd, cancellationToken);

        return Results.Ok(new UpdateUserClaimsResponse(res.UserId, res.UpdatedClaims.Select(c => new UserClaimRequest(c.Type, c.Value)).ToList(), res.RemovedClaims.Select(c => c.Type + ":" + c.Value).ToList(), res.UpdatedAt));
    }

    private async Task<IResult> RemoveUserClaim(Guid userId, string claimType, string claimValue, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.RemoveUserClaim.RemoveUserClaimCommand(userId, claimType, claimValue),
            cancellationToken);

        return Results.Ok(new RemoveUserClaimResponse(res.UserId, res.ClaimType, res.ClaimValue, res.Removed));
    }

    // ------------------------------------------------------------------
    // Roles
    // ------------------------------------------------------------------

    private async Task<IResult> GetUserRoles(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserRoles.GetUserRolesQuery(userId), cancellationToken);
        return Results.Ok(new GetUserRolesResponse(res.UserId, res.Roles.ToList()));
    }

    private async Task<IResult> UpdateUserRoles(Guid userId, UpdateUserRolesRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.UpdateUserRoles.UpdateUserRolesCommand(userId, request.RoleNames),
            cancellationToken);

        return Results.Ok(new UpdateUserRolesResponse(res.UserId, res.Roles.ToList(), res.UpdatedAt));
    }

    // ------------------------------------------------------------------
    // Security & Status
    // ------------------------------------------------------------------

    private async Task<IResult> LockUser(Guid userId, LockUserRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.LockUser.LockUserCommand(userId, request.LockoutEnd),
            cancellationToken);

        return Results.Ok(new LockUserResponse(res.UserId, res.LockoutEnd));
    }

    private async Task<IResult> UnlockUser(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.UnlockUser.UnlockUserCommand(userId),
            cancellationToken);

        return Results.Ok(new UnlockUserResponse(res.UserId, res.IsUnlocked));
    }

    private async Task<IResult> ActivateUser(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.ActivateUser.ActivateUserCommand(userId),
            cancellationToken);

        return Results.Ok(new ActivateUserResponse(res.UserId, res.IsActive));
    }

    private async Task<IResult> DeactivateUser(Guid userId, DeactivateUserRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.DeactivateUser.DeactivateUserCommand(userId, request.Reason),
            cancellationToken);

        return Results.Ok(new DeactivateUserResponse(res.UserId, res.IsActive));
    }

    private async Task<IResult> UpdateUserPassword(Guid userId, UpdateUserPasswordRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.UpdateUserPassword.UpdateUserPasswordCommand(userId, request.NewPassword),
            cancellationToken);

        if (!res.Succeeded)
        {
            return Results.BadRequest(new UpdateUserPasswordResponse(res.UserId, false, res.Error));
        }

        return Results.Ok(new UpdateUserPasswordResponse(res.UserId, true, null));
    }
}
