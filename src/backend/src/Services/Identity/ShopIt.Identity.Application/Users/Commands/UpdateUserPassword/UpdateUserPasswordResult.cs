namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPassword;

public record UpdateUserPasswordResult(Guid UserId, bool Succeeded, string? Error);
