using FluentValidation;

namespace ShopIt.Identity.Application.Roles.Queries.GetRole;

public class GetRoleQueryValidator : AbstractValidator<GetRoleQuery>
{
    public GetRoleQueryValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
