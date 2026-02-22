using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.API.Features;

public record UserResponse(Guid Id, string Username, string Email);

public record GetUserQuery(Guid UserId) : IQuery<UserResponse>;

public class GetUserQueryHandler(ILogger<GetUserQueryHandler> logger) : IQueryHandler<GetUserQuery, UserResponse>
{
    private readonly ILogger<GetUserQueryHandler> _logger = logger;

    public async Task<UserResponse> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        await Task.Yield();

        _logger.LogInformation("Retrieving user with ID: {UserId}", query.UserId);
        // Implement the logic to retrieve the user by ID and return a UserResponse
        return new UserResponse(query.UserId, "Test", "test@example.com");
    }
}
