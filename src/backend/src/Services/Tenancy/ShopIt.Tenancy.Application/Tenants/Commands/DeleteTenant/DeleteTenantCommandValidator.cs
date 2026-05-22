using FluentValidation;

namespace ShopIt.Tenancy.Application.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
