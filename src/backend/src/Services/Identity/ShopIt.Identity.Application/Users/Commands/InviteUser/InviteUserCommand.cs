using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Application.Users.Commands.CreateUser;

namespace ShopIt.Identity.Application.Users.Commands.InviteUser;

/// <summary>
/// Provisions a new user via the admin invitation flow: the account is created in
/// <c>PendingActivation</c> state (no password), an activation token is issued, and a
/// notification event is published so the Notifications service delivers the invitation email.
/// </summary>
public record InviteUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber = null,
    IEnumerable<string>? Roles = null,
    IEnumerable<CreateUserClaimItem>? Claims = null) : ICommand<InviteUserResult>;
