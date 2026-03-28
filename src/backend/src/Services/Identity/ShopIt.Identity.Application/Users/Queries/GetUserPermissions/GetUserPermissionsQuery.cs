using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Users.Queries.GetUserPermissions;

public record GetUserPermissionsQuery(Guid UserId) : IQuery<GetUserPermissionsResult>;

