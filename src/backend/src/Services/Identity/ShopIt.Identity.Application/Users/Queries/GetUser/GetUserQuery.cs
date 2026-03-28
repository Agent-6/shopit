using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Application.Users.Queries.GetUser;

namespace ShopIt.Identity.Application.Users.Queries.GetUser;

public record GetUserQuery(Guid UserId) : IQuery<GetUserResult>;

