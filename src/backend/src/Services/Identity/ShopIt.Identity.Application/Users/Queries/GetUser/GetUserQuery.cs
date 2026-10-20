using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Application.Users.Queries.GetUser;

namespace ShopIt.Identity.Application.Users.Queries.GetUser;

public record GetUserQuery(Guid UserId) : IQuery<Result<GetUserResult, Error>>;

