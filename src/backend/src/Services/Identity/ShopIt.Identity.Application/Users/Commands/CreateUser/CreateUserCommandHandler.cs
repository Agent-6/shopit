using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler(
    UserManager<User> userManager,
    ICurrentTenant currentTenant) : ICommandHandler<CreateUserCommand, Result<CreateUserResult, Error>>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<CreateUserResult, Error>> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();

        var user = User.Create(
            userId,
            request.Email,
            request.Username,
            _currentTenant.Id);

        // Set optional properties
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.UpdateProfile(request.FirstName, request.LastName ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.SetPhoneNumber(request.PhoneNumber);

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return UserErrors.Invalid("User.Create", result.Errors.Select(e => e.Description));
        }

        if (request.Roles is not null && request.Roles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
            {
                // The user exists now. Leaving it behind without the roles it was created with would be
                // a half-applied command, so remove it before reporting the failure.
                await _userManager.DeleteAsync(user);
                return UserErrors.Invalid("User.Create", roleResult.Errors.Select(e => e.Description));
            }
        }

        if (request.Claims is not null)
        {
            foreach (var claim in request.Claims)
            {
                var claimResult = await _userManager.AddClaimAsync(user, new Claim(claim.Type, claim.Value));
                if (!claimResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return UserErrors.Invalid("User.Create", claimResult.Errors.Select(e => e.Description));
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
