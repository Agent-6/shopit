namespace ShopIt.Identity.Presentation.Users.Responses;

public record GetUserRolesResponse(Guid UserId, List<string> Roles);

public record UpdateUserRolesResponse(Guid UserId, List<string> Roles, DateTime UpdatedAt);

public record LockUserResponse(Guid UserId, DateTimeOffset? LockoutEnd);

public record UnlockUserResponse(Guid UserId, bool IsUnlocked);

public record ActivateUserResponse(Guid UserId, bool IsActive);

public record DeactivateUserResponse(Guid UserId, bool IsActive);

public record UpdateUserPasswordResponse(Guid UserId, bool Succeeded, string? Error);

public record GetMyPermissionsResponse(IReadOnlyCollection<string> Permissions);
