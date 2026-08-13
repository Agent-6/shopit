using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.RemoveUserClaim;

public record RemoveUserClaimCommand(Guid UserId, string ClaimType, string ClaimValue) : ICommand<RemoveUserClaimResult>;
