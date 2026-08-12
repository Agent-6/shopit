using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.UnlockUser;

public class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
