namespace ShopIt.Identity.Presentation.Users;

public record DeleteUserResponse(
    Guid Id,
    bool IsDeleted,
    string DeletedType
);
