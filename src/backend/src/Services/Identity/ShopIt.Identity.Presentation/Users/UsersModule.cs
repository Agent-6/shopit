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

    private async Task<IResult> GetMyPermissions(IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.QueryAsync(
            new ShopIt.Identity.Application.Users.Queries.GetMyPermissions.GetMyPermissionsQuery(),
            cancellationToken);

        // An unauthenticated principal is an Unauthorized error now, not an exception.
        return result.ToHttpResult(
            onSuccess: res => Results.Ok(new GetMyPermissionsResponse(res.Permissions.ToList())));
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
        if (user.IsFailure)
        {
            return user.Error.ToProblem();
        }

        var claims = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserClaims.GetUserClaimsQuery(userId), cancellationToken);
        if (claims.IsFailure)
        {
            return claims.Error.ToProblem();
        }

        var roles = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserRoles.GetUserRolesQuery(userId), cancellationToken);
        if (roles.IsFailure)
        {
            return roles.Error.ToProblem();
        }

        var response = new UserDetailResponse(
            Id: user.Value.Id,
            Username: user.Value.Username,
            Email: user.Value.Email,
            FirstName: user.Value.FirstName,
            LastName: user.Value.LastName,
            IsActive: user.Value.IsActive,
            Status: user.Value.Status,
            EmailConfirmed: user.Value.EmailConfirmed,
            PhoneNumber: user.Value.PhoneNumber,
            PhoneNumberConfirmed: user.Value.PhoneNumberConfirmed,
            TwoFactorEnabled: user.Value.TwoFactorEnabled,
            LockoutEnabled: user.Value.LockoutEnabled,
            LockoutEnd: user.Value.LockoutEnd,
            CreatedAt: user.Value.CreatedAt,
            LastModifiedAt: user.Value.LastModifiedAt,
            Roles: roles.Value.Roles.ToList(),
            Claims: claims.Value.Claims.Select(c => new UserClaimResponse(c.Type, c.Value)).ToList()
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

        return result.ToHttpResult(
            onSuccess: created => Results.Created(
                $"/users/{created.Id}",
                new CreateUserResponse(created.Id, created.Username, created.Email, DateTime.UtcNow)));
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

        return result.ToHttpResult(
            onSuccess: invited => Results.Created(
                $"/users/{invited.Id}",
                new InviteUserResponse(invited.Id, invited.Email, invited.Status, invited.InvitationExpiresAt)));
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

        return res.ToHttpResult(
            onSuccess: updated => Results.Ok(new UpdateUserResponse(
                Id: updated.Id,
                Username: updated.Username,
                Email: updated.Email,
                FirstName: updated.FirstName,
                LastName: updated.LastName,
                PhoneNumber: updated.PhoneNumber,
                IsActive: updated.IsActive,
                LastModifiedAt: updated.LastModifiedAt)));
    }

    private async Task<IResult> DeleteUser(Guid userId, [FromQuery] bool permanent = false, IDispatcher dispatcher = null!, CancellationToken cancellationToken = default)
    {
        var cmd = new ShopIt.Identity.Application.Users.Commands.DeleteUser.DeleteUserCommand(userId, permanent);
        var res = await dispatcher.SendAsync(cmd, cancellationToken);

        return res.ToHttpResult(
            onSuccess: deleted => Results.Ok(new DeleteUserResponse(deleted.Id, deleted.IsDeleted, deleted.DeletedType)));
    }

    // ------------------------------------------------------------------
    // Permissions
    // ------------------------------------------------------------------

    private async Task<IResult> GetUserPermissions(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var perms = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserPermissions.GetUserPermissionsQuery(userId), cancellationToken);

        return perms.ToHttpResult(onSuccess: permissions =>
        {
            var direct = permissions.Permissions
                .Select(p => new UserPermissionResponse(p.PermissionName, p.IsGranted, p.Source == "Role" ? PermissionSource.Role : PermissionSource.Direct))
                .ToList();
            var inherited = permissions.InheritedPermissions
                .Select(ip => new InheritedPermissionResponse(ip.Permission, ip.Source))
                .ToList();

            return Results.Ok(new GetUserPermissionsResponse(userId, direct, inherited));
        });
    }

    private async Task<IResult> UpdateUserPermissions(Guid userId, UpdateUserPermissionsRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var items = request.Permissions.Select(p => new ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions.PermissionUpdateItem(p.PermissionName, p.IsGranted));
        var cmd = new ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions.UpdateUserPermissionsCommand(userId, items);
        var res = await dispatcher.SendAsync(cmd, cancellationToken);

        return res.ToHttpResult(
            onSuccess: updated => Results.Ok(new UpdateUserPermissionsResponse(
                updated.UserId,
                updated.GrantedPermissions.ToList(),
                updated.RevokedPermissions.ToList(),
                updated.UpdatedAt)));
    }

    // ------------------------------------------------------------------
    // Roles
    // ------------------------------------------------------------------

    private async Task<IResult> GetUserRoles(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserRoles.GetUserRolesQuery(userId), cancellationToken);
        return res.ToHttpResult(
            onSuccess: roles => Results.Ok(new GetUserRolesResponse(roles.UserId, roles.Roles.ToList())));
    }

    private async Task<IResult> UpdateUserRoles(Guid userId, UpdateUserRolesRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.UpdateUserRoles.UpdateUserRolesCommand(userId, request.RoleNames),
            cancellationToken);

        return res.ToHttpResult(
            onSuccess: roles => Results.Ok(new UpdateUserRolesResponse(roles.UserId, roles.Roles.ToList(), roles.UpdatedAt)));
    }

    // ------------------------------------------------------------------
    // Security & Status
    // ------------------------------------------------------------------

    private async Task<IResult> LockUser(Guid userId, LockUserRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.LockUser.LockUserCommand(userId, request.LockoutEnd),
            cancellationToken);

        return res.ToHttpResult(
            onSuccess: locked => Results.Ok(new LockUserResponse(locked.UserId, locked.LockoutEnd)));
    }

    private async Task<IResult> UnlockUser(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.UnlockUser.UnlockUserCommand(userId),
            cancellationToken);

        return res.ToHttpResult(
            onSuccess: unlocked => Results.Ok(new UnlockUserResponse(unlocked.UserId, unlocked.IsUnlocked)));
    }

    private async Task<IResult> ActivateUser(Guid userId, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.ActivateUser.ActivateUserCommand(userId),
            cancellationToken);

        return res.ToHttpResult(
            onSuccess: activated => Results.Ok(new ActivateUserResponse(activated.UserId, activated.IsActive)));
    }

    private async Task<IResult> DeactivateUser(Guid userId, DeactivateUserRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.DeactivateUser.DeactivateUserCommand(userId, request.Reason),
            cancellationToken);

        return res.ToHttpResult(
            onSuccess: deactivated => Results.Ok(new DeactivateUserResponse(deactivated.UserId, deactivated.IsActive)));
    }

    private async Task<IResult> UpdateUserPassword(Guid userId, UpdateUserPasswordRequest request, ShopIt.Framework.Core.CQRS.IDispatcher dispatcher, CancellationToken cancellationToken = default)
    {
        var res = await dispatcher.SendAsync(
            new ShopIt.Identity.Application.Users.Commands.UpdateUserPassword.UpdateUserPasswordCommand(userId, request.NewPassword),
            cancellationToken);

        return res.ToHttpResult(
            onSuccess: changed => Results.Ok(new UpdateUserPasswordResponse(changed.UserId, true, null)));
    }
}
