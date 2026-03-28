namespace ShopIt.Identity.Application.Users.Commands.DeleteUser;

public record DeleteUserResult(Guid Id, bool IsDeleted, string DeletedType);
