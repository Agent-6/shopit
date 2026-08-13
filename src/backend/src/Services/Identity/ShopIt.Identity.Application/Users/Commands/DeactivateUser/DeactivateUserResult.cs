namespace ShopIt.Identity.Application.Users.Commands.DeactivateUser;

public record DeactivateUserResult(Guid UserId, bool IsActive);
