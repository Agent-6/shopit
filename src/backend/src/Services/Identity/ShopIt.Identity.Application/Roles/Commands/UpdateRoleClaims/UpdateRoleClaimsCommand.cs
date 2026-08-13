using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRoleClaims;

public record RoleClaimUpdateItem(string Type, string Value);

public record UpdateRoleClaimsCommand(Guid RoleId, IEnumerable<RoleClaimUpdateItem> Claims) : ICommand<UpdateRoleClaimsResult>;
