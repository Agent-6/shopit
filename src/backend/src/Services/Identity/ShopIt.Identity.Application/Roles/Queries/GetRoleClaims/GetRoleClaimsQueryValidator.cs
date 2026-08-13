using FluentValidation;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoleClaims;

public class GetRoleClaimsQueryValidator : AbstractValidator<GetRoleClaimsQuery>
{
    public GetRoleClaimsQueryValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
