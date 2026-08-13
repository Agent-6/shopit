namespace ShopIt.Identity.Presentation.Users.Requests;

public record UpdateUserRolesRequest(
    List<string> RoleNames
);

public record LockUserRequest(
    DateTimeOffset? LockoutEnd = null
);

public record DeactivateUserRequest(
    string? Reason = null
);

public record UpdateUserPasswordRequest(
    string NewPassword
);
