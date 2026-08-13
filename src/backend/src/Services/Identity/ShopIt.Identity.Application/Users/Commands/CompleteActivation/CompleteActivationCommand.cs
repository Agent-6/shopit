using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.CompleteActivation;

/// <summary>
/// Completes the invitation flow: validates the activation token, stores the user's chosen
/// password and activates the account. Called synchronously by the Authentication service
/// from the "Set Your Password" page.
/// </summary>
public record CompleteActivationCommand(
    Guid UserId,
    string Token,
    string Password) : ICommand<CompleteActivationResult>;
