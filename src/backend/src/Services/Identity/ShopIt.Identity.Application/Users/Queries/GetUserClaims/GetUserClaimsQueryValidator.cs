using FluentValidation;

namespace ShopIt.Identity.Application.Users.Queries.GetUserClaims;

public class GetUserClaimsQueryValidator : AbstractValidator<GetUserClaimsQuery>
{
    public GetUserClaimsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
