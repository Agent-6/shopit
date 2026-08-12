namespace ShopIt.Identity.Application.Roles.Commands.UpdateRole;

public record UpdateRoleResult(Guid Id, string Name, string? Description, DateTime UpdatedAt);
