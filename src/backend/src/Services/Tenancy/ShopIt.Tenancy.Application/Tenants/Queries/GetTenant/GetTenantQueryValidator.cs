using FluentValidation;

namespace ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;

public class GetTenantQueryValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
