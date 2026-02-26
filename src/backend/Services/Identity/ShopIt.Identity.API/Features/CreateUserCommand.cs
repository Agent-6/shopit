using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.API.Features;

public record CreateUserCommand(string Username, string Email) : ICommand<Guid>;

public class CreateUserCommandHandler(ILogger<CreateUserCommandHandler> logger) : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly ILogger<CreateUserCommandHandler> _logger = logger;

    public Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with Username: {Username}, and Email: {Email}", command.Username, command.Email);
        // Implement the logic to create a user and return the new user's ID
        var newUserId = Guid.NewGuid(); // Placeholder for actual user creation logic
        return Task.FromResult(newUserId);
    }
}
