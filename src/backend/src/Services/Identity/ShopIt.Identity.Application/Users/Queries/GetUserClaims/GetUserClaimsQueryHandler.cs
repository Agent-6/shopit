using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Queries.GetUserClaims;

public class GetUserClaimsQueryHandler : IQueryHandler<GetUserClaimsQuery, GetUserClaimsResult>
{
    private readonly UserManager<User> _userManager;

    public GetUserClaimsQueryHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<GetUserClaimsResult> HandleAsync(GetUserClaimsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var claims = await _userManager.GetClaimsAsync(user);
        var items = claims.Select(c => new UserClaimItem(c.Type, c.Value));

        return new GetUserClaimsResult(request.UserId, items);
    }
}
