using FluentValidation;
using ShopIt.Tenancy.Domain.Repositories;

namespace ShopIt.Tenancy.Application.Tenants.Commands.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator(ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(async (name, cancellationToken) => !await tenantRepository.ExistsByNameAsync(name, cancellationToken))
            .WithMessage("A tenant with the specified name already exists.");
    }
}
