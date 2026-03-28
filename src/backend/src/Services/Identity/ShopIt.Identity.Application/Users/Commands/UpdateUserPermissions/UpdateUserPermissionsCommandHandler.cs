using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions;

public class UpdateUserPermissionsCommandHandler : ICommandHandler<UpdateUserPermissionsCommand, UpdateUserPermissionsResult>
{
    private readonly UserManager<User> _userManager;

    public UpdateUserPermissionsCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UpdateUserPermissionsResult> HandleAsync(UpdateUserPermissionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var granted = new List<string>();
        var revoked = new List<string>();

        var existingClaims = (await _userManager.GetClaimsAsync(user)).ToList();

        foreach (var p in request.Permissions)
        {
            var existing = existingClaims.FirstOrDefault(c => c.Type == p.PermissionName);
            if (p.IsGranted && existing == null)
            {
                var res = await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(p.PermissionName, "true"));
                if (res.Succeeded) granted.Add(p.PermissionName);
            }
            else if (!p.IsGranted && existing != null)
            {
                var res = await _userManager.RemoveClaimAsync(user, existing);
                if (res.Succeeded) revoked.Add(p.PermissionName);
            }
        }

        return new UpdateUserPermissionsResult(request.UserId, granted, revoked, DateTime.UtcNow);
    }
}
