using FluentValidation;

namespace ShopIt.Tenancy.Application.Tenants.Commands.ActivateTenant;

public class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
    public ActivateTenantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
