namespace ShopIt.Identity.Application.Users.Commands.UpdateUser;

public record UpdateUserResult(Guid Id, string Username, string Email, string? FirstName, string? LastName, string? PhoneNumber, bool IsActive, DateTime LastModifiedAt);
