using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Users.Queries.GetUserClaims;

public record GetUserClaimsQuery(Guid UserId) : IQuery<GetUserClaimsResult>;

