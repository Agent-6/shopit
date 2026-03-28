using FluentValidation;

namespace ShopIt.Identity.Application.Users.Queries.GetUserPermissions;

public class GetUserPermissionsQueryValidator : AbstractValidator<GetUserPermissionsQuery>
{
    public GetUserPermissionsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
