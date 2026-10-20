using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Application.Users.Activation;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.InviteUser;

public class InviteUserCommandHandler(
    UserManager<User> userManager,
    ICurrentTenant currentTenant,
    IActivationTokenProvider tokenProvider) : ICommandHandler<InviteUserCommand, Result<InviteUserResult, Error>>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IActivationTokenProvider _tokenProvider = tokenProvider;

    public async Task<Result<InviteUserResult, Error>> HandleAsync(InviteUserCommand request, CancellationToken cancellationToken)
    {
        // Emails are used as the login identifier (consistent with seeding and login).
        var email = request.Email.Trim();
        var userId = Guid.NewGuid();

        var activationToken = _tokenProvider.Issue(userId);

        var user = User.Invite(
            userId,
            email,
            userName: email,
            _currentTenant.Id,
            activationToken.Token,
            activationToken.ExpiresAt);

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.UpdateProfile(request.FirstName, request.LastName ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.SetPhoneNumber(request.PhoneNumber);

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return UserErrors.Invalid("User.Invite", result.Errors.Select(e => e.Description));
        }

        if (request.Roles is not null && request.Roles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
            {
                // The user exists now; remove it rather than leave a half-applied invitation.
                await _userManager.DeleteAsync(user);
                return UserErrors.Invalid("User.Invite", roleResult.Errors.Select(e => e.Description));
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
                    return UserErrors.Invalid("User.Invite", claimResult.Errors.Select(e => e.Description));
                }
            }
        }

        // The UserInvitedDomainEvent raised by User.Invite() is dispatched on commit; the
        // UserInvitedEventHandler publishes the invitation notification into the outbox.
        return new InviteUserResult(
            user.Id,
            user.Email!,
            user.Status.ToString(),
            activationToken.ExpiresAt);
    }
}
