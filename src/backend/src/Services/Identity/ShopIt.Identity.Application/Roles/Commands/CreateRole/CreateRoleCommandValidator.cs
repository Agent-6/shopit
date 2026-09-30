using FluentValidation;
using Microsoft.AspNetCore.Identity;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Roles.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator(RoleManager<Role> roleManager)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        // Uniqueness is scoped to the tenant: RoleManager resolves through the tenant-aware role
        // store, so the same name in a different tenant is not a collision. The database enforces
        // this too via the (NormalizedName, TenantId) unique index; checking here turns a constraint
        // violation into a readable validation error.
        RuleFor(x => x.Name)
            .MustAsync(async (name, cancellationToken) =>
                await roleManager.FindByNameAsync(name) is null)
            .WithMessage("A role with the specified name already exists.");
    }
}
