using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.Users.Queries.GetUser;

public class GetUserQueryHandler(
    UserManager<User> userManager,
    ICurrentTenant currentTenant,
    ILogger<GetUserQueryHandler> logger) : IQueryHandler<GetUserQuery, GetUserResult>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ILogger<GetUserQueryHandler> _logger = logger;

    public async Task<GetUserResult> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetUserQuery for UserId: {UserId}", request.UserId);
        _logger.LogInformation("Current Tenant: {TenantId}", _currentTenant.Id);

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
            throw new KeyNotFoundException("User not found");

        return new GetUserResult(
            Id: user.Id,
            Username: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsActive: user.IsActive,
            Status: user.Status.ToString(),
            EmailConfirmed: user.EmailConfirmed,
            PhoneNumber: user.PhoneNumber,
            PhoneNumberConfirmed: user.PhoneNumberConfirmed,
            TwoFactorEnabled: user.TwoFactorEnabled,
            LockoutEnabled: user.LockoutEnabled,
            LockoutEnd: user.LockoutEnd,
            AccessFailedCount: user.AccessFailedCount,
            CreatedAt: user.CreatedAt,
            LastModifiedAt: user.LastModifiedAt
        );
    }
}
