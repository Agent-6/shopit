using FluentValidation;

namespace ShopIt.Identity.Application.Users.Commands.RemoveUserClaim;

public class RemoveUserClaimCommandValidator : AbstractValidator<RemoveUserClaimCommand>
{
    public RemoveUserClaimCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ClaimType).NotEmpty();
        RuleFor(x => x.ClaimValue).NotEmpty();
    }
}
