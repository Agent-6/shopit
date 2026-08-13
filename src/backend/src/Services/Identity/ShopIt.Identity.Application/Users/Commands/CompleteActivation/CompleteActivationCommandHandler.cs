using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Application.Users.Activation;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Enums;

namespace ShopIt.Identity.Application.Users.Commands.CompleteActivation;

public class CompleteActivationCommandHandler(
    UserManager<User> userManager,
    IActivationTokenProvider tokenProvider) : ICommandHandler<CompleteActivationCommand, CompleteActivationResult>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IActivationTokenProvider _tokenProvider = tokenProvider;

    public async Task<CompleteActivationResult> HandleAsync(CompleteActivationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return new CompleteActivationResult(
                Succeeded: false,
                UserId: request.UserId,
                TenantId: Guid.Empty,
                UserName: string.Empty,
                Email: string.Empty,
                ErrorCode: "USER_NOT_FOUND",
                Error: "User not found.");
        }

        var validation = _tokenProvider.Validate(request.UserId, request.Token);
        if (validation.IsExpired)
        {
            return new CompleteActivationResult(
                false, user.Id, user.TenantId, user.UserName!, user.Email!,
                ErrorCode: "ACTIVATION_TOKEN_EXPIRED", Error: validation.Error);
        }

        if (!validation.IsValid)
        {
            return new CompleteActivationResult(
                false, user.Id, user.TenantId, user.UserName!, user.Email!,
                ErrorCode: "ACTIVATION_TOKEN_INVALID", Error: validation.Error);
        }

        // Idempotent replay: a previously activated account clicking the link again is
        // still signed in (the caller owns the email address proven by the token).
        if (user.Status == UserStatus.Active && user.EmailConfirmed && !string.IsNullOrEmpty(user.PasswordHash))
        {
            return new CompleteActivationResult(true, user.Id, user.TenantId, user.UserName!, user.Email!);
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            var addResult = await _userManager.AddPasswordAsync(user, request.Password);
            if (!addResult.Succeeded)
            {
                return new CompleteActivationResult(
                    false, user.Id, user.TenantId, user.UserName!, user.Email!,
                    ErrorCode: "PASSWORD_POLICY",
                    Error: string.Join("; ", addResult.Errors.Select(e => e.Description)));
            }
        }

        user.CompleteActivation();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return new CompleteActivationResult(
                false, user.Id, user.TenantId, user.UserName!, user.Email!,
                ErrorCode: "ACTIVATION_FAILED",
                Error: string.Join("; ", updateResult.Errors.Select(e => e.Description)));
        }

        return new CompleteActivationResult(true, user.Id, user.TenantId, user.UserName!, user.Email!);
    }
}
