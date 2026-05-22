using FluentValidation;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeactivateTenant;

public class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    public DeactivateTenantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
