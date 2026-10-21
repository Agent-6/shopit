using FluentValidation;

namespace ShopIt.Tenancy.Application.Tenants.Commands.ChangeTenantCurrency;

/// <summary>
/// Rejects a request that cannot name a currency at all. The code's <em>shape</em> is the aggregate's rule
/// (<c>Tenant.ValidateCurrency</c>), which the handler reports as a <c>Result</c> failure; this validator only
/// stops the request before the database read a missing id or currency would make pointless.
/// </summary>
public class ChangeTenantCurrencyCommandValidator : AbstractValidator<ChangeTenantCurrencyCommand>
{
    public ChangeTenantCurrencyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Currency)
            .NotEmpty();

        RuleFor(x => x.Currency)
            .MaximumLength(3)
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
    }
}
