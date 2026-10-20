using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Queries.GetUserClaims;

public class GetUserClaimsQueryHandler : IQueryHandler<GetUserClaimsQuery, Result<GetUserClaimsResult, Error>>
{
    private readonly UserManager<User> _userManager;

    public GetUserClaimsQueryHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<GetUserClaimsResult, Error>> HandleAsync(GetUserClaimsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound(request.UserId);
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var items = claims.Select(c => new UserClaimItem(c.Type, c.Value));

        return new GetUserClaimsResult(request.UserId, items);
    }
}
