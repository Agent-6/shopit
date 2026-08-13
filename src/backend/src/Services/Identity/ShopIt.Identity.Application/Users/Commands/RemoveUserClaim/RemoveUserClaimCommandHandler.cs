using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.RemoveUserClaim;

public class RemoveUserClaimCommandHandler(UserManager<User> userManager) : ICommandHandler<RemoveUserClaimCommand, RemoveUserClaimResult>
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<RemoveUserClaimResult> HandleAsync(RemoveUserClaimCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var claim = new System.Security.Claims.Claim(request.ClaimType, request.ClaimValue);
        var result = await _userManager.RemoveClaimAsync(user, claim);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to remove the claim: {errors}");
        }

        return new RemoveUserClaimResult(user.Id, request.ClaimType, request.ClaimValue, true);
    }
}
