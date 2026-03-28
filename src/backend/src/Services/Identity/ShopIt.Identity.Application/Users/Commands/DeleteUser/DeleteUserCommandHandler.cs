using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Repositories;

namespace ShopIt.Identity.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, DeleteUserResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;

    public DeleteUserCommandHandler(UserManager<User> userManager, IUserRepository userRepository)
    {
        _userManager = userManager;
        _userRepository = userRepository;
    }

    public async Task<DeleteUserResult> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        if (request.Permanent)
        {
            var res = await _userManager.DeleteAsync(user);
            if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
            return new DeleteUserResult(user.Id, true, "Permanent");
        }
        else
        {
            user.Deactivate("soft-delete");
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) throw new InvalidOperationException(string.Join(";", res.Errors.Select(e => e.Description)));
            return new DeleteUserResult(user.Id, true, "Soft");
        }
    }
}
