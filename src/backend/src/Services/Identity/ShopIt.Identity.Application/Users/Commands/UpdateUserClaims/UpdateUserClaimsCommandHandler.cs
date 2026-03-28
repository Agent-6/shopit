using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserClaims;

public class UpdateUserClaimsCommandHandler : ICommandHandler<UpdateUserClaimsCommand, UpdateUserClaimsResult>
{
    private readonly UserManager<User> _userManager;

    public UpdateUserClaimsCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UpdateUserClaimsResult> HandleAsync(UpdateUserClaimsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var updated = new List<UserClaimUpdateItem>();
        var removed = new List<UserClaimUpdateItem>();

        // Add or replace claims
        foreach (var c in request.Claims)
        {
            var existing = (await _userManager.GetClaimsAsync(user)).Where(ec => ec.Type == c.Type).ToList();
            foreach (var ex in existing)
            {
                await _userManager.RemoveClaimAsync(user, ex);
            }

            var res = await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(c.Type, c.Value));
            if (res.Succeeded) updated.Add(c);
        }

        // Remove specified claims
        if (request.RemovedClaims is not null)
        {
            foreach (var rc in request.RemovedClaims)
            {
                var existing = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(ec => ec.Type == rc.Type && ec.Value == rc.Value);
                if (existing is not null)
                {
                    var res = await _userManager.RemoveClaimAsync(user, existing);
                    if (res.Succeeded) removed.Add(rc);
                }
            }
        }

        return new UpdateUserClaimsResult(request.UserId, updated, removed, DateTime.UtcNow);
    }
}
