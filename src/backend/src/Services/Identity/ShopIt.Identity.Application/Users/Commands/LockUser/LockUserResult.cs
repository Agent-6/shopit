namespace ShopIt.Identity.Application.Users.Commands.LockUser;

public record LockUserResult(Guid UserId, DateTimeOffset? LockoutEnd);
