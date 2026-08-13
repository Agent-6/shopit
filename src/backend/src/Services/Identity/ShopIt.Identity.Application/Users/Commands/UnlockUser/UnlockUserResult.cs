namespace ShopIt.Identity.Application.Users.Commands.UnlockUser;

public record UnlockUserResult(Guid UserId, bool IsUnlocked);
