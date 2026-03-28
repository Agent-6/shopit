using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions;

public class UpdateUserPermissionsCommandValidator : AbstractValidator<UpdateUserPermissionsCommand>
{
    public UpdateUserPermissionsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();
        RuleForEach(x => x.Permissions).ChildRules(p => {
            p.RuleFor(x => x.PermissionName).NotEmpty();
        });
    }
}
