using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Events.RoleEvents;

namespace ShopIt.Identity.Application.Roles.EventHandlers;

/// <summary>
/// When a role is created, finds every user holding an admin role in the same tenant and
/// assigns the new role to them — so admins automatically inherit newly created roles
/// (ABP-style role seeding behavior).
/// </summary>
public class RoleCreatedEventHandler(
    UserManager<User> userManager,
    ILogger<RoleCreatedEventHandler> logger) : IDomainEventHandler<RoleCreatedDomainEvent>
{
    public async Task HandleAsync(RoleCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var role = domainEvent.Role;
        if (role.Name is null)
            return;

        // Host roles are seeded at startup, not user-created; the "new role → all admins"
        // rule applies to tenant-scoped roles only.
        if (role.TenantId == Guid.Empty)
            return;

        // The create-role request scope already sets ICurrentTenant, so this finds admins
        // within the same tenant only.
        var admins = await userManager.GetUsersInRoleAsync("Admin");

        foreach (var admin in admins)
        {
            if (!await userManager.IsInRoleAsync(admin, role.Name))
            {
                await userManager.AddToRoleAsync(admin, role.Name);
            }
        }

        logger.LogInformation(
            "Assigned new role '{Role}' to {Count} admin user(s) in tenant {TenantId}.",
            role.Name, admins.Count, role.TenantId);
    }
}
