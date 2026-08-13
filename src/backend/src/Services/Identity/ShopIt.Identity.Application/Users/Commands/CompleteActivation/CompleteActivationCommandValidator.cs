using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.CompleteActivation;

public class CompleteActivationCommandValidator : AbstractValidator<CompleteActivationCommand>
{
    public CompleteActivationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);
    }
}
