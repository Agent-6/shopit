using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler(
    IUserRepository userRepository,
    UserManager<User> userManager) : IQueryHandler<GetUsersQuery, GetUsersResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<GetUsersResult> HandleAsync(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (users, total) = await _userRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.Filter, cancellationToken);

        var userList = users.ToList();
        var rolesByUser = await _userRepository.GetRoleNamesForUsersAsync(userList.Select(u => u.Id), cancellationToken);

        var items = userList.Select(u => new GetUsersUserItem(
            u.Id,
            u.UserName ?? string.Empty,
            u.Email ?? string.Empty,
            u.FirstName,
            u.LastName,
            u.IsActive,
            u.Status.ToString(),
            u.EmailConfirmed,
            u.PhoneNumber,
            u.PhoneNumberConfirmed,
            u.LockoutEnabled,
            u.LockoutEnd,
            u.CreatedAt,
            u.LastModifiedAt,
            rolesByUser.TryGetValue(u.Id, out var roles) ? roles : []
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);

        return new GetUsersResult(items, total, request.PageNumber, request.PageSize, totalPages);
    }
}
