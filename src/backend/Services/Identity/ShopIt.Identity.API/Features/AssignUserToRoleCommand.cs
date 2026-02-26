using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.API.Features;

public record AssignUserToRoleCommand : ICommand<string>;

public class AssignUserToRoleCommandHandler : ICommandHandler<AssignUserToRoleCommand, string>
{
    public Task<string> HandleAsync(AssignUserToRoleCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult("User assigned to role successfully.");
    }
}
