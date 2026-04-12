using System;

namespace ShopIt.Authentication.Application.Models;

public record CredentialValidationResult(Guid UserId, string UserName, string Email);
