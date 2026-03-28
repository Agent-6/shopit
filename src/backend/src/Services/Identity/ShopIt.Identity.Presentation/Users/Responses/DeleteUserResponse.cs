namespace ShopIt.Identity.Presentation.Users.Responses;

public record DeleteUserResponse(
    Guid Id,
    bool IsDeleted,
    string DeletedType
);
