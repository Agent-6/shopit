namespace ShopIt.Identity.Presentation.Users.Responses;

public record GetUsersResponse
{
    public required List<UserResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
