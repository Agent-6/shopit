using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.DeactivateUser;

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
