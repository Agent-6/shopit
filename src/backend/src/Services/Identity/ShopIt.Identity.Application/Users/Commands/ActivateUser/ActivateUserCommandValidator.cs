using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.ActivateUser;

public class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
