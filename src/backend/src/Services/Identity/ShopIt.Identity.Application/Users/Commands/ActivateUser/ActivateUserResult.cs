namespace ShopIt.Identity.Application.Users.Commands.ActivateUser;

public record ActivateUserResult(Guid UserId, bool IsActive);
