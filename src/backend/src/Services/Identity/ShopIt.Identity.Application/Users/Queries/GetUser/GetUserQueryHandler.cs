using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Queries.GetUser;

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, GetUserResult>
{
    private readonly UserManager<User> _userManager;

    public GetUserQueryHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<GetUserResult> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
            throw new KeyNotFoundException("User not found");

        return new GetUserResult(
            Id: user.Id,
            Username: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsActive: user.IsActive
        );
    }
}
