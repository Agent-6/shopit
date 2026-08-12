using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.LockUser;

public class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        When(x => x.LockoutEnd.HasValue, () =>
        {
            RuleFor(x => x.LockoutEnd)
                .Must(end => end > DateTimeOffset.UtcNow)
                .WithMessage("Lockout end must be in the future.");
        });
    }
}
