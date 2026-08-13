using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserRoles;

public class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleForEach(x => x.RoleNames).NotEmpty().MaximumLength(100);
    }
}
