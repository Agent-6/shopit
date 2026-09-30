using FluentValidation;
using Microsoft.AspNetCore.Identity;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRole;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator(RoleManager<Role> roleManager)
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        // One rule rather than two so the role is loaded once.
        RuleFor(x => x.Name)
            .CustomAsync(async (name, context, cancellationToken) =>
            {
                var role = await roleManager.FindByIdAsync(context.InstanceToValidate.RoleId.ToString());

                // A missing role is the handler's concern; it reports not-found more precisely.
                if (role is null || string.Equals(role.Name, name, StringComparison.Ordinal))
                {
                    return;
                }

                if (role.IsStatic)
                {
                    context.AddFailure("Built-in roles cannot be renamed.");
                    return;
                }

                var existing = await roleManager.FindByNameAsync(name);
                if (existing is not null && existing.Id != role.Id)
                {
                    context.AddFailure("A role with the specified name already exists.");
                }
            });
    }
}
