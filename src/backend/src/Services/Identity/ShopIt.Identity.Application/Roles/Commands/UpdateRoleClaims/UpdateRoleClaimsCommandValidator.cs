using FluentValidation;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRoleClaims;

public class UpdateRoleClaimsCommandValidator : AbstractValidator<UpdateRoleClaimsCommand>
{
    public UpdateRoleClaimsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleForEach(x => x.Claims)
            .ChildRules(claim =>
            {
                claim.RuleFor(c => c.Type).NotEmpty();
                claim.RuleFor(c => c.Value).NotEmpty();
            });
    }
}
