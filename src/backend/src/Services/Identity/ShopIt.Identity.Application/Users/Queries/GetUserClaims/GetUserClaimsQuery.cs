using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Users.Queries.GetUserClaims;

public record GetUserClaimsQuery(Guid UserId) : IQuery<Result<GetUserClaimsResult, Error>>;

