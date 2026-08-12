using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler(
    UserManager<User> userManager,
    ICurrentTenant currentTenant) : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<CreateUserResult> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();

        var user = User.Create(
            userId,
            request.Email,
            request.Username,
            _currentTenant.Id,
            createdBy: "system"
        );

        // Set optional properties
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.UpdateProfile(request.FirstName, request.LastName ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.SetPhoneNumber(request.PhoneNumber);

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        if (request.Roles is not null && request.Roles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign roles: {errors}");
            }
        }

        if (request.Claims is not null)
        {
            foreach (var claim in request.Claims)
            {
                var claimResult = await _userManager.AddClaimAsync(user, new Claim(claim.Type, claim.Value));
                if (!claimResult.Succeeded)
                {
                    var errors = string.Join("; ", claimResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign claim '{claim.Type}': {errors}");
                }
            }
        }

        return new CreateUserResult(
            Id: user.Id,
            Username: user.UserName!,
            Email: user.Email!,
            FirstName: user.FirstName ?? string.Empty,
            LastName: user.LastName ?? string.Empty,
            PhoneNumber: user.PhoneNumber
        );
    }
}
