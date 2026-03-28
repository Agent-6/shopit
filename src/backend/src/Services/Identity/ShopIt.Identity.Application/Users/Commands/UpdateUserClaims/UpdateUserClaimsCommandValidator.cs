using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserClaims;

public class UpdateUserClaimsCommandValidator : AbstractValidator<UpdateUserClaimsCommand>
{
    public UpdateUserClaimsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Claims).NotNull();
        RuleForEach(x => x.Claims).ChildRules(c => {
            c.RuleFor(x => x.Type).NotEmpty();
            c.RuleFor(x => x.Value).NotEmpty();
        });

        When(x => x.RemovedClaims is not null, () =>
        {
            RuleForEach(x => x.RemovedClaims).ChildRules(c => {
                c.RuleFor(x => x.Type).NotEmpty();
                c.RuleFor(x => x.Value).NotEmpty();
            });
        });
    }
}
