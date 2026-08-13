using FluentValidation;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRolePermissions;

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();
        RuleForEach(x => x.Permissions).ChildRules(p =>
        {
            p.RuleFor(x => x.PermissionName).NotEmpty();
        });
    }
}
