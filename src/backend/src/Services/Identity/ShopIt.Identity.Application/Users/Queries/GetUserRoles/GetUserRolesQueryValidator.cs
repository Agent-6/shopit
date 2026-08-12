using FluentValidation;

namespace ShopIt.Identity.Application.Users.Queries.GetUserRoles;

public class GetUserRolesQueryValidator : AbstractValidator<GetUserRolesQuery>
{
    public GetUserRolesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
