using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Modules;
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
        // User CRUD Operations
        app.MapGet("/", GetUsers).RequireAuthorization();
        app.MapGet("/{userId:guid}", GetUserById).RequireAuthorization();
        app.MapPost("/", CreateUser).RequireAuthorization();
        app.MapPut("/{userId:guid}", UpdateUser).RequireAuthorization();
        app.MapDelete("/{userId:guid}", DeleteUser).RequireAuthorization();
        // User Permissions
        app.MapGet("/{userId:guid}/permissions", GetUserPermissions).RequireAuthorization();
        app.MapPut("/{userId:guid}/permissions", UpdateUserPermissions).RequireAuthorization();

        // User Claims
        app.MapGet("/{userId:guid}/claims", GetUserClaims).RequireAuthorization();
        app.MapPut("/{userId:guid}/claims", UpdateUserClaims).RequireAuthorization();
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
                EmailConfirmed: false,
                PhoneNumber: null,
                PhoneNumberConfirmed: false,
                LockoutEnabled: false,
                LockoutEnd: null,
                CreatedAt: DateTime.UtcNow,
                LastModifiedAt: DateTime.UtcNow
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
        var perms = await dispatcher.QueryAsync(new ShopIt.Identity.Application.Users.Queries.GetUserPermissions.GetUserPermissionsQuery(userId), cancellationToken);

        var roles = perms.InheritedPermissions.Select(p => p.Source).Distinct().ToList();

        var response = new UserDetailResponse(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsActive: user.IsActive,
            EmailConfirmed: false,
            PhoneNumber: null,
            PhoneNumberConfirmed: false,
            TwoFactorEnabled: false,
            LockoutEnabled: false,
            LockoutEnd: null,
            CreatedAt: DateTime.UtcNow,
            LastModifiedAt: DateTime.UtcNow,
            Roles: roles,
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
            request.PhoneNumber
        );

        var result = await dispatcher.SendAsync(cmd, cancellationToken);

        var response = new CreateUserResponse(result.Id, result.Username, result.Email, DateTime.UtcNow);
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
            request.IsActive
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
}
