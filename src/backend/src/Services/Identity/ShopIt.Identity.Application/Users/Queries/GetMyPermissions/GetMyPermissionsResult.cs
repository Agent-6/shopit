namespace ShopIt.Identity.Application.Users.Queries.GetMyPermissions;

public record GetMyPermissionsResult(IReadOnlyCollection<string> Permissions);
