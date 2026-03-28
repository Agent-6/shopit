using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, GetUsersResult>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetUsersResult> HandleAsync(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (users, total) = await _userRepository.GetPagedAsync(request.Page, request.PageSize, request.Filter, cancellationToken);

        var items = users.Select(u => new GetUsersUserItem(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, u.FirstName, u.LastName, u.IsActive));

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);

        return new GetUsersResult(items, total, request.Page, request.PageSize, totalPages);
    }
}
