using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserClaims;

public record UserClaimUpdateItem(string Type, string Value);

public record UpdateUserClaimsCommand(Guid UserId, IEnumerable<UserClaimUpdateItem> Claims, IEnumerable<UserClaimUpdateItem>? RemovedClaims) : ICommand<UpdateUserClaimsResult>;

